using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      // Satu hotspot siap tampil. Diperkuat = ada laporan warga terverifikasi dalam 5 km.
      public record HotspotDasbor(TitikPanas Titik, TingkatConfidence Tingkat, string LabelConfidence, bool Diperkuat);

      public class KabupatenDasbor
      {
            public Wilayah Wilayah { get; init; } = new();
            public List<HotspotDasbor> Hotspot { get; init; } = new();
            public DataCuaca? Cuaca { get; init; }
            public IndeksRisiko? Risiko { get; init; }
            public int Jumlah => Hotspot.Count;
      }

      public class DataDasbor
      {
            public string Provinsi { get; init; } = string.Empty;
            public List<KabupatenDasbor> Kabupaten { get; init; } = new();
            public Dictionary<int, List<List<(double Lat, double Lon)>>> Batas { get; init; } = new();

            public int Hotspot24Jam { get; init; }
            public int Hotspot24JamSebelumnya { get; init; }
            public int ConfidenceTinggi { get; init; }
            public KabupatenDasbor? RisikoTertinggi { get; init; }
            public int LaporanHariIni { get; init; }
            public int LaporanCocok { get; init; }

            // Catatan sinkronisasi terakhir per sumber ("FIRMS", "Cuaca", "Email").
            public Dictionary<string, CatatanSinkron> Sinkron { get; init; } = new();
      }

      public class SeriHarian
      {
            public List<(DateOnly Tanggal, int Jumlah)> Hari { get; init; } = new();
            public int Total { get; init; }
            public int Puncak { get; init; }
            public DateOnly TanggalPuncak { get; init; }
            public double RataRata { get; init; }
            public double RataRataPekanLalu { get; init; }
      }

      // Membaca database dan merangkumnya untuk halaman Dashboard serta Riwayat & Tren.
      public class DasborService
      {
            // Waktu Indonesia Barat. Semua waktu di database UTC.
            public static readonly TimeSpan Wib = TimeSpan.FromHours(7);

            private readonly WilayahRepository _wilayahRepo;
            private readonly TitikPanasRepository _titikRepo;
            private readonly CuacaRepository _cuacaRepo;
            private readonly IndeksRisikoRepository _risikoRepo;
            private readonly LaporanWargaRepository _laporanRepo;
            private readonly PengaturanRepository _logRepo;
            private readonly AgregatHotspotRepository _agregatRepo;

            public DasborService(KoneksiDatabase db)
            {
                  _agregatRepo = new AgregatHotspotRepository(db);
                  _wilayahRepo = new WilayahRepository(db);
                  _titikRepo = new TitikPanasRepository(db);
                  _cuacaRepo = new CuacaRepository(db);
                  _risikoRepo = new IndeksRisikoRepository(db);
                  _laporanRepo = new LaporanWargaRepository(db);
                  _logRepo = new PengaturanRepository(db);
            }

            public static DateOnly HariIniWib => DateOnly.FromDateTime(DateTime.UtcNow + Wib);

            public async Task<List<string>> AmbilProvinsiAsync()
            {
                  return (await _wilayahRepo.AmbilProvinsiAsync()).Select(p => p.Nama).ToList();
            }

            public async Task<List<Wilayah>> AmbilKabupatenAsync(string provinsi)
            {
                  return (await _wilayahRepo.AmbilSemuaAsync())
                        .Where(w => w.Tingkat == TingkatWilayah.Kabupaten && w.Provinsi == provinsi)
                        .OrderBy(w => w.Nama)
                        .ToList();
            }

            // "Mode lihat nasional": ringkasan tiap provinsi yang ada di database,
            // dihitung oleh AgregasiNasionalService (hanya baca).
            public async Task<List<(Wilayah Provinsi, RingkasanProvinsi Ringkasan, int JumlahKabupaten)>> MuatRingkasanNasionalAsync()
            {
                  var semua = await _wilayahRepo.AmbilSemuaAsync();
                  var agregasi = new AgregasiNasionalService(_agregatRepo);

                  var hasil = new List<(Wilayah, RingkasanProvinsi, int)>();
                  foreach (var provinsi in semua.Where(w => w.Tingkat == TingkatWilayah.Provinsi).OrderBy(w => w.Nama))
                  {
                        var ringkasan = await agregasi.HitungRingkasan(provinsi);
                        hasil.Add((provinsi, ringkasan, semua.Count(w => w.WilayahIndukId == provinsi.Id)));
                  }
                  return hasil.OrderByDescending(h => h.Item2.JumlahHotspot).ThenBy(h => h.Item1.Nama).ToList();
            }

            public Task<Dictionary<string, CatatanSinkron>> AmbilSinkronTerakhirAsync()
            {
                  return _logRepo.AmbilSinkronTerakhirAsync();
            }

            public async Task<DataDasbor> MuatAsync(string provinsi)
            {
                  var sekarang = DateTime.UtcNow;

                  var kabupatenTask = AmbilKabupatenAsync(provinsi);
                  var titikTask = _titikRepo.AmbilSejakAsync(sekarang.AddHours(-48));
                  var cuacaTask = _cuacaRepo.AmbilSemuaAsync();
                  var risikoTask = _risikoRepo.AmbilSemuaAsync();
                  var laporanTask = _laporanRepo.AmbilSemuaAsync();
                  var batasTask = _wilayahRepo.AmbilBatasAsync();
                  var sinkronTask = _logRepo.AmbilSinkronTerakhirAsync();
                  await Task.WhenAll(kabupatenTask, titikTask, cuacaTask, risikoTask, laporanTask, batasTask, sinkronTask);

                  var kabupaten = kabupatenTask.Result;
                  var idKabupaten = kabupaten.Select(w => w.Id).ToHashSet();
                  var titik = titikTask.Result.Where(t => idKabupaten.Contains(t.WilayahId)).ToList();
                  var batas24 = sekarang.AddHours(-24);
                  var titik24 = titik.Where(t => t.WaktuDeteksi >= batas24).ToList();

                  var cuaca = cuacaTask.Result.ToDictionary(c => c.WilayahId);
                  var risiko = risikoTask.Result.ToDictionary(r => r.WilayahId);

                  var laporan = laporanTask.Result.Where(l => idKabupaten.Contains(l.WilayahId)).ToList();
                  var terverifikasi = laporan.Where(l => l.Status == StatusLaporan.Diverifikasi).ToList();
                  var laporanHariIni = laporan.Where(l => DateOnly.FromDateTime(l.DibuatPada + Wib) == HariIniWib).ToList();

                  var daftar = kabupaten.Select(w => new KabupatenDasbor
                  {
                        Wilayah = w,
                        Cuaca = cuaca.GetValueOrDefault(w.Id),
                        Risiko = risiko.GetValueOrDefault(w.Id),
                        Hotspot = titik24
                              .Where(t => t.WilayahId == w.Id)
                              .Select(t => new HotspotDasbor(
                                    t,
                                    Confidence.Tingkat(t.Confidence),
                                    Confidence.Label(t.Confidence),
                                    // Aturan domain yang sama dengan verifikasi laporan: radius 5 km.
                                    terverifikasi.Any(l => new LaporanWarga { Lat = l.Lat, Lon = l.Lon }.Verifikasi(t))))
                              .OrderByDescending(h => h.Tingkat)
                              .ThenByDescending(h => h.Titik.WaktuDeteksi)
                              .ToList()
                  }).ToList();

                  return new DataDasbor
                  {
                        Provinsi = provinsi,
                        Kabupaten = daftar,
                        Batas = batasTask.Result.Where(b => idKabupaten.Contains(b.Key)).ToDictionary(b => b.Key, b => b.Value),
                        Hotspot24Jam = titik24.Count,
                        Hotspot24JamSebelumnya = titik.Count - titik24.Count,
                        ConfidenceTinggi = titik24.Count(t => Confidence.Tingkat(t.Confidence) == TingkatConfidence.Tinggi),
                        RisikoTertinggi = daftar.Where(k => k.Risiko is not null).OrderByDescending(k => k.Risiko!.Skor).FirstOrDefault(),
                        LaporanHariIni = laporanHariIni.Count,
                        LaporanCocok = laporanHariIni.Count(l => l.Status == StatusLaporan.Diverifikasi),
                        Sinkron = sinkronTask.Result
                  };
            }

            // Jumlah hotspot per hari (tanggal WIB) selama `hari` hari terakhir, untuk
            // seluruh provinsi (wilayahId null) atau satu kabupaten. Hari tanpa data
            // tetap muncul dengan nilai 0. Tujuh hari sebelum rentang ikut dihitung
            // sebagai pembanding rata-rata.
            public async Task<SeriHarian> MuatRiwayatAsync(string provinsi, int? wilayahId, int hari = 14)
            {
                  var hariIni = HariIniWib;
                  var awal = hariIni.AddDays(-(hari - 1));
                  var awalPembanding = awal.AddDays(-7);

                  // Tengah malam WIB tanggal awalPembanding, dalam UTC.
                  var sejakUtc = awalPembanding.ToDateTime(TimeOnly.MinValue) - Wib;
                  var perHari = await _titikRepo.HitungPerHariAsync(sejakUtc, provinsi, wilayahId);

                  var seri = Enumerable.Range(0, hari)
                        .Select(i => awal.AddDays(i))
                        .Select(t => (t, perHari.GetValueOrDefault(t)))
                        .ToList();

                  int puncak = seri.Max(s => s.Item2);
                  var pekanTerakhir = seri.Skip(Math.Max(0, hari - 7)).Select(s => (double)s.Item2).ToList();
                  var pekanSebelumnya = Enumerable.Range(0, 7)
                        .Select(i => (double)perHari.GetValueOrDefault(hariIni.AddDays(-13 + i)))
                        .ToList();

                  return new SeriHarian
                  {
                        Hari = seri,
                        Total = seri.Sum(s => s.Item2),
                        Puncak = puncak,
                        TanggalPuncak = seri.Last(s => s.Item2 == puncak).t,
                        RataRata = pekanTerakhir.Average(),
                        RataRataPekanLalu = pekanSebelumnya.Average()
                  };
            }
      }
}
