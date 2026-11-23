using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      // Hasil banyak kali simulasi: untuk tiap jam, peluang tiap sel sudah terbakar.
      public class HasilMonteCarlo
      {
            public Wilayah Wilayah { get; init; } = new();
            public List<TitikPanas> Klaster { get; init; } = new();
            public DataCuaca Cuaca { get; init; } = new();
            public int JumlahHotspotWilayah { get; init; }
            public bool MemakaiRentang7Hari { get; init; }

            public int Baris { get; init; }
            public int Kolom { get; init; }
            public double LatAsal { get; init; }   // sudut barat daya grid
            public double LonAsal { get; init; }
            public double UkuranSel { get; init; } // derajat
            public double LuasSelHa { get; init; }
            public int JumlahSimulasi { get; init; }
            public int Jam { get; init; }

            // Peluang[t][baris * Kolom + kolom], t = 0..Jam, nilai 0..1.
            public double[][] Peluang { get; init; } = Array.Empty<double[]>();

            public double LuasHa(int jam, double ambang)
            {
                  return Peluang[Math.Clamp(jam, 0, Jam)].Count(p => p >= ambang) * LuasSelHa;
            }
      }

      // Menjalankan SimulasiPenyebaran (cellular automaton milik Nabil) berkali-kali
      // untuk satu klaster hotspot, lalu merangkumnya jadi peta peluang.
      public class SimulasiService
      {
            public const int JamSimulasi = 6;
            public const int JumlahSimulasiBawaan = 50;

            // Hotspot dalam radius ini dari hotspot pemicu dianggap satu klaster.
            private const double RadiusKlasterKm = 10;

            private readonly WilayahRepository _wilayahRepo;
            private readonly TitikPanasRepository _titikRepo;
            private readonly CuacaRepository _cuacaRepo;
            private readonly HasilSimulasiRepository _hasilRepo;

            public SimulasiService(KoneksiDatabase db)
            {
                  _wilayahRepo = new WilayahRepository(db);
                  _titikRepo = new TitikPanasRepository(db);
                  _cuacaRepo = new CuacaRepository(db);
                  _hasilRepo = new HasilSimulasiRepository(db);
            }

            // Kabupaten yang punya hotspot dalam 7 hari terakhir, terbanyak dulu.
            public async Task<List<(Wilayah Wilayah, int Hotspot24Jam, int Hotspot7Hari)>> AmbilWilayahBerhotspotAsync(string provinsi)
            {
                  var sekarang = DateTime.UtcNow;
                  var kabupaten = (await _wilayahRepo.AmbilSemuaAsync())
                        .Where(w => w.Tingkat == TingkatWilayah.Kabupaten && w.Provinsi == provinsi)
                        .ToList();
                  var titik = await _titikRepo.AmbilSejakAsync(sekarang.AddDays(-7));

                  return kabupaten
                        .Select(w => (w,
                              titik.Count(t => t.WilayahId == w.Id && t.WaktuDeteksi >= sekarang.AddHours(-24)),
                              titik.Count(t => t.WilayahId == w.Id)))
                        .Where(x => x.Item3 > 0)
                        .OrderByDescending(x => x.Item2)
                        .ThenByDescending(x => x.Item3)
                        .ThenBy(x => x.w.Nama)
                        .ToList();
            }

            public async Task<HasilMonteCarlo> JalankanAsync(int wilayahId, int jumlahSimulasi = JumlahSimulasiBawaan)
            {
                  var wilayah = (await _wilayahRepo.AmbilSemuaAsync()).FirstOrDefault(w => w.Id == wilayahId)
                                ?? throw new InvalidOperationException("Wilayah tidak ditemukan.");

                  var sekarang = DateTime.UtcNow;
                  var titik7 = (await _titikRepo.AmbilSejakAsync(sekarang.AddDays(-7))).Where(t => t.WilayahId == wilayahId).ToList();
                  var titik24 = titik7.Where(t => t.WaktuDeteksi >= sekarang.AddHours(-24)).ToList();

                  // Utamakan hotspot 24 jam; kalau tidak ada, pakai 7 hari dan beri tahu pemakai.
                  bool pakai7Hari = titik24.Count == 0;
                  var sumber = pakai7Hari ? titik7 : titik24;
                  if (sumber.Count == 0)
                        throw new InvalidOperationException($"Tidak ada hotspot di {wilayah.Nama} dalam 7 hari terakhir, jadi tidak ada klaster untuk disimulasikan.");

                  var cuaca = (await _cuacaRepo.AmbilSemuaAsync()).FirstOrDefault(c => c.WilayahId == wilayahId)
                              ?? throw new InvalidOperationException($"Belum ada data cuaca untuk {wilayah.Nama}. Tekan \"Perbarui data\" di Dashboard terlebih dahulu.");

                  // Klaster = hotspot dalam 10 km dari hotspot pemicu (confidence tertinggi, lalu terbaru).
                  var pemicu = sumber.OrderByDescending(t => Confidence.Tingkat(t.Confidence)).ThenByDescending(t => t.WaktuDeteksi).First();
                  var klaster = sumber.Where(t => Geo.JarakKm(pemicu.Lat, pemicu.Lon, t.Lat, t.Lon) <= RadiusKlasterKm).ToList();

                  var hasil = await Task.Run(() => MonteCarlo(wilayah, klaster, cuaca, jumlahSimulasi, pakai7Hari, sumber.Count));

                  await _hasilRepo.SimpanAsync(new HasilSimulasi
                  {
                        ParameterAngin = cuaca.KecepatanAngin,
                        ParameterKelembapan = cuaca.Kelembapan,
                        WilayahId = wilayah.Id,
                        SnapshotGrid = JsonSerializer.Serialize(new
                        {
                              hasil.Baris,
                              hasil.Kolom,
                              hasil.LatAsal,
                              hasil.LonAsal,
                              hasil.UkuranSel,
                              hasil.JumlahSimulasi,
                              hasil.Jam,
                              ArahAngin = cuaca.ArahAngin,
                              JumlahHotspot = klaster.Count,
                              // Peluang pada jam terakhir, dibulatkan dua angka supaya ringkas.
                              PeluangAkhir = hasil.Peluang[hasil.Jam].Select(p => Math.Round(p, 2)).ToArray()
                        })
                  });

                  return hasil;
            }

            // Tidak menyentuh database, jadi bisa dites sendiri.
            public static HasilMonteCarlo MonteCarlo(Wilayah wilayah, List<TitikPanas> klaster, DataCuaca cuaca, int jumlahSimulasi, bool pakai7Hari = false, int? jumlahHotspotWilayah = null)
            {
                  // Ruang di sekeliling klaster = jumlah jam + 2, supaya api yang merambat
                  // satu sel per jam tidak terpotong tepi grid.
                  var generator = new GridGenerator(JamSimulasi + 2);
                  double ukuran = GridGenerator.UkuranSelDerajat;
                  double latAsal = klaster.Min(t => t.Lat) - generator.Buffer * ukuran;
                  double lonAsal = klaster.Min(t => t.Lon) - generator.Buffer * ukuran;

                  int baris = 0, kolom = 0;
                  double[][] hitung = Array.Empty<double[]>();

                  for (int s = 0; s < jumlahSimulasi; s++)
                  {
                        var simulasi = new SimulasiPenyebaran(wilayah, klaster, cuaca, generator);
                        for (int t = 0; t <= JamSimulasi; t++)
                        {
                              // JalankanSampai(0) hanya mengembalikan grid awal; JalankanSampai(1) maju satu jam.
                              var grid = simulasi.JalankanSampai(t == 0 ? 0 : 1);
                              if (hitung.Length == 0)
                              {
                                    baris = grid.Baris;
                                    kolom = grid.Kolom;
                                    hitung = Enumerable.Range(0, JamSimulasi + 1).Select(_ => new double[baris * kolom]).ToArray();
                              }

                              for (int i = 0; i < grid.Peta.Count; i++)
                              {
                                    if (grid.Peta[i].Status is StatusSel.Terbakar or StatusSel.Habis)
                                          hitung[t][i]++;
                              }
                        }
                  }

                  foreach (var perJam in hitung)
                  {
                        for (int i = 0; i < perJam.Length; i++)
                        {
                              perJam[i] /= jumlahSimulasi;
                        }
                  }

                  double latTengah = klaster.Average(t => t.Lat);
                  double sisiKmUtara = ukuran * 111.32;
                  double sisiKmTimur = ukuran * 111.32 * Math.Cos(latTengah * Math.PI / 180.0);

                  return new HasilMonteCarlo
                  {
                        Wilayah = wilayah,
                        Klaster = klaster,
                        Cuaca = cuaca,
                        MemakaiRentang7Hari = pakai7Hari,
                        JumlahHotspotWilayah = jumlahHotspotWilayah ?? klaster.Count,
                        Baris = baris,
                        Kolom = kolom,
                        LatAsal = latAsal,
                        LonAsal = lonAsal,
                        UkuranSel = ukuran,
                        LuasSelHa = sisiKmUtara * sisiKmTimur * 100.0,
                        JumlahSimulasi = jumlahSimulasi,
                        Jam = JamSimulasi,
                        Peluang = hitung
                  };
            }
      }
}
