using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public class HasilSinkron
      {
            public int HotspotBaru { get; set; }
            public int CuacaTersimpan { get; set; }
            public int EmailTerkirim { get; set; }
            public int EmailGagal { get; set; }
            public int EmailDilewati { get; set; }
            public List<string> Kesalahan { get; } = new();

            public string Ringkasan()
            {
                  string teks = $"{HotspotBaru} hotspot baru, cuaca {CuacaTersimpan} wilayah";
                  if (EmailTerkirim + EmailGagal + EmailDilewati > 0)
                        teks += $", email: {EmailTerkirim} terkirim, {EmailGagal} gagal, {EmailDilewati} dilewati";
                  return Kesalahan.Count == 0 ? teks + "." : teks + ". Masalah: " + string.Join(" | ", Kesalahan);
            }
      }

      // Mengambil data dari luar lalu menyimpannya ke database:
      //   1. hotspot FIRMS  -> titik_panas (hanya yang belum ada)
      //   2. cuaca terkini  -> data_cuaca
      //   3. indeks risiko  -> indeks_risiko (KalkulatorRisiko)
      //   4. peringatan email ke pelanggan (NotifikasiService)
      // Tiap langkah berdiri sendiri: kalau FIRMS gagal, cuaca tetap diambil,
      // dan setiap hasil dicatat di log_sinkronisasi.
      public class SinkronisasiService
      {
            // Hanya satu sinkronisasi berjalan pada satu waktu di aplikasi ini
            // (tombol "Perbarui data" dan pengatur waktu otomatis bisa bertabrakan).
            private static readonly SemaphoreSlim Gerbang = new(1, 1);

            private readonly KoneksiDatabase _db;
            private readonly PengaturanService _pengaturan;
            private readonly HttpClient _http;
            private readonly WilayahRepository _wilayahRepo;
            private readonly TitikPanasRepository _titikRepo;
            private readonly CuacaRepository _cuacaRepo;
            private readonly IndeksRisikoRepository _risikoRepo;
            private readonly SubscriberRepository _subscriberRepo;
            private readonly RiwayatNotifikasiRepository _riwayatRepo;
            private readonly PengaturanRepository _logRepo;

            public SinkronisasiService(KoneksiDatabase db, PengaturanService pengaturan, HttpClient? http = null)
            {
                  _db = db;
                  _pengaturan = pengaturan;
                  _http = http ?? KlienHttp.Bersama;
                  _wilayahRepo = new WilayahRepository(db);
                  _titikRepo = new TitikPanasRepository(db);
                  _cuacaRepo = new CuacaRepository(db);
                  _risikoRepo = new IndeksRisikoRepository(db);
                  _subscriberRepo = new SubscriberRepository(db);
                  _riwayatRepo = new RiwayatNotifikasiRepository(db);
                  _logRepo = new PengaturanRepository(db);
            }

            // riwayat7Hari = true mengambil deteksi 7 hari (dipakai sekali untuk
            // mengisi grafik Riwayat & Tren), selain itu 24 jam terakhir.
            public async Task<HasilSinkron> JalankanAsync(bool riwayat7Hari = false)
            {
                  await Gerbang.WaitAsync();
                  try
                  {
                        var hasil = new HasilSinkron();
                        var lokal = _pengaturan.MuatLokal();
                        var semuaWilayah = await _wilayahRepo.AmbilSemuaAsync();
                        var kabupaten = semuaWilayah.Where(w => w.Tingkat == TingkatWilayah.Kabupaten).ToList();

                        var baru = await AmbilHotspotAsync(hasil, lokal, semuaWilayah, kabupaten, riwayat7Hari);
                        var cuaca = await AmbilCuacaAsync(hasil, lokal, kabupaten);
                        await HitungRisikoAsync(hasil, kabupaten, cuaca);
                        await KirimPeringatanAsync(hasil, lokal, kabupaten, baru);
                        return hasil;
                  }
                  finally
                  {
                        Gerbang.Release();
                  }
            }

            private async Task<List<TitikPanas>> AmbilHotspotAsync(
                  HasilSinkron hasil, PengaturanLokal lokal, List<Wilayah> semuaWilayah, List<Wilayah> kabupaten, bool riwayat7Hari)
            {
                  try
                  {
                        List<TitikPanas> mentah;
                        string sumber;
                        if (!string.IsNullOrWhiteSpace(lokal.FirmsMapKey))
                        {
                              // POLYMORPHISM: dua sumber hotspot dipanggil lewat tipe dasar yang sama.
                              SumberData<List<TitikPanas>> api = new FirmsApiService(_http, lokal.FirmsMapKey.Trim());
                              mentah = new List<TitikPanas>();
                              foreach (var provinsi in semuaWilayah.Where(w => w.Tingkat == TingkatWilayah.Provinsi))
                              {
                                    mentah.AddRange(await api.AmbilData(provinsi));
                              }
                              sumber = "FIRMS API (MAP_KEY)";
                        }
                        else
                        {
                              var publik = new FirmsPublikService(_http, riwayat7Hari ? RentangFirms.Hari7 : RentangFirms.Jam24);
                              mentah = await publik.AmbilSemua();
                              sumber = riwayat7Hari ? "FIRMS publik 7 hari" : "FIRMS publik 24 jam";
                        }

                        var penentu = new PenentuWilayah(kabupaten, await _wilayahRepo.AmbilBatasAsync());
                        var dikenali = new List<TitikPanas>();
                        var sudah = new HashSet<(double, double, DateTime, string)>();
                        foreach (var t in mentah)
                        {
                              var w = penentu.Tentukan(t.Lat, t.Lon);
                              if (w is null || !sudah.Add((t.Lat, t.Lon, t.WaktuDeteksi, t.Satelit)))
                                    continue;
                              t.WilayahId = w.Id;
                              dikenali.Add(t);
                        }

                        var baru = await _titikRepo.SimpanBaruAsync(dikenali);
                        hasil.HotspotBaru = baru.Count;
                        await _logRepo.CatatSinkronAsync("FIRMS", true, baru.Count,
                              $"{sumber}: {mentah.Count} deteksi diunduh, {dikenali.Count} di wilayah yang dipantau, {baru.Count} baru");
                        return baru;
                  }
                  catch (Exception ex)
                  {
                        hasil.Kesalahan.Add("FIRMS: " + ex.Message);
                        await CatatGagalAsync("FIRMS", ex);
                        return new List<TitikPanas>();
                  }
            }

            private async Task<List<DataCuaca>> AmbilCuacaAsync(HasilSinkron hasil, PengaturanLokal lokal, List<Wilayah> kabupaten)
            {
                  try
                  {
                        List<DataCuaca> cuaca;
                        if (!string.IsNullOrWhiteSpace(lokal.AccuWeatherKey))
                        {
                              // POLYMORPHISM: AccuWeather dan Open-Meteo sama-sama SumberData<DataCuaca>.
                              SumberData<DataCuaca> api = new CuacaApiService(_http, lokal.AccuWeatherKey.Trim());
                              cuaca = new List<DataCuaca>();
                              foreach (var w in kabupaten)
                              {
                                    var c = await api.AmbilData(w);
                                    c.Sumber = "AccuWeather";
                                    // AccuWeather memberi jam lokal; database menyimpan UTC.
                                    c.Waktu = DateTime.UtcNow;
                                    cuaca.Add(c);
                              }
                        }
                        else
                        {
                              cuaca = await new OpenMeteoService(_http).AmbilBanyak(kabupaten);
                        }

                        hasil.CuacaTersimpan = await _cuacaRepo.SimpanBanyakAsync(cuaca);
                        await _logRepo.CatatSinkronAsync("Cuaca", true, cuaca.Count,
                              $"{(cuaca.Count > 0 ? cuaca[0].Sumber : "-")}: {cuaca.Count} wilayah");
                        return cuaca;
                  }
                  catch (Exception ex)
                  {
                        hasil.Kesalahan.Add("Cuaca: " + ex.Message);
                        await CatatGagalAsync("Cuaca", ex);
                        return new List<DataCuaca>();
                  }
            }

            // Indeks risiko per kabupaten = cuaca terkini + jumlah hotspot 24 jam.
            private async Task HitungRisikoAsync(HasilSinkron hasil, List<Wilayah> kabupaten, List<DataCuaca> cuaca)
            {
                  if (cuaca.Count == 0)
                        return;

                  try
                  {
                        var hotspot24 = await _titikRepo.AmbilSejakAsync(DateTime.UtcNow.AddHours(-24));
                        var jumlah = hotspot24.GroupBy(t => t.WilayahId).ToDictionary(g => g.Key, g => g.Count());
                        var kalkulator = new KalkulatorRisiko();

                        var indeks = new List<IndeksRisiko>();
                        foreach (var c in cuaca)
                        {
                              int skor = kalkulator.Hitung(c, jumlah.GetValueOrDefault(c.WilayahId));
                              indeks.Add(new IndeksRisiko { Skor = skor, Status = kalkulator.TentukanStatus(skor), WilayahId = c.WilayahId });
                        }
                        await _risikoRepo.SimpanBanyakAsync(indeks);
                  }
                  catch (Exception ex)
                  {
                        hasil.Kesalahan.Add("Indeks risiko: " + ex.Message);
                  }
            }

            // Satu email per pelanggan per sinkronisasi, untuk hotspot baru dengan
            // confidence tertinggi di wilayahnya. Hanya deteksi 24 jam terakhir yang
            // memicu peringatan, supaya pengisian riwayat 7 hari tidak mengirim email basi.
            // Bisa dipanggil sendiri (mis. dari tes) untuk daftar hotspot yang sudah tersimpan.
            public async Task<HasilSinkron> KirimPeringatanAsync(List<TitikPanas> baru)
            {
                  var hasil = new HasilSinkron();
                  var kabupaten = (await _wilayahRepo.AmbilSemuaAsync()).Where(w => w.Tingkat == TingkatWilayah.Kabupaten).ToList();
                  await KirimPeringatanAsync(hasil, _pengaturan.MuatLokal(), kabupaten, baru);
                  return hasil;
            }

            private async Task KirimPeringatanAsync(HasilSinkron hasil, PengaturanLokal lokal, List<Wilayah> kabupaten, List<TitikPanas> baru)
            {
                  try
                  {
                        var batas = DateTime.UtcNow.AddHours(-24);
                        var perWilayah = baru
                              .Where(t => t.WaktuDeteksi >= batas)
                              .GroupBy(t => t.WilayahId)
                              .ToDictionary(g => g.Key, g => g
                                    .OrderByDescending(t => Confidence.Tingkat(t.Confidence))
                                    .ThenByDescending(t => t.WaktuDeteksi)
                                    .First());

                        foreach (var (wilayahId, titik) in perWilayah)
                        {
                              var wilayah = kabupaten.FirstOrDefault(w => w.Id == wilayahId);
                              if (wilayah is null)
                                    continue;

                              foreach (var pelanggan in await _subscriberRepo.AmbilAktifPerWilayahAsync(wilayahId))
                              {
                                    if (!lokal.SmtpLengkap)
                                    {
                                          await CatatAsync(pelanggan, titik, HasilNotifikasi.Dilewati, "SMTP belum diatur di komputer ini");
                                          hasil.EmailDilewati++;
                                          continue;
                                    }

                                    try
                                    {
                                          // NotifikasiService (milik Nabil) yang mengirim email dan mencatat
                                          // riwayatnya lewat IRiwayatNotifikasiStore = RiwayatNotifikasiRepository.
                                          var layanan = new NotifikasiService(_riwayatRepo, new PengaturanSmtp
                                          {
                                                Host = lokal.SmtpHost.Trim(),
                                                Port = lokal.SmtpPort,
                                                EmailPengirim = lokal.SmtpEmailPengirim.Trim(),
                                                Username = lokal.SmtpUsername.Trim(),
                                                Password = lokal.SmtpPassword
                                          });
                                          var tujuan = new Wilayah { Id = wilayah.Id, Nama = wilayah.Nama, Subscriber = new List<Subscriber> { pelanggan } };
                                          await layanan.CekDanKirim(tujuan, new List<TitikPanas> { titik });
                                          hasil.EmailTerkirim++;
                                    }
                                    catch (Exception ex)
                                    {
                                          await CatatAsync(pelanggan, titik, HasilNotifikasi.Gagal, ex.Message);
                                          hasil.EmailGagal++;
                                    }
                              }
                        }

                        if (hasil.EmailTerkirim + hasil.EmailGagal + hasil.EmailDilewati > 0)
                        {
                              await _logRepo.CatatSinkronAsync("Email", hasil.EmailGagal == 0, hasil.EmailTerkirim,
                                    $"{hasil.EmailTerkirim} terkirim, {hasil.EmailGagal} gagal, {hasil.EmailDilewati} dilewati");
                        }
                  }
                  catch (Exception ex)
                  {
                        hasil.Kesalahan.Add("Email: " + ex.Message);
                        await CatatGagalAsync("Email", ex);
                  }
            }

            private Task CatatAsync(Subscriber pelanggan, TitikPanas titik, HasilNotifikasi hasilKirim, string keterangan)
            {
                  return _riwayatRepo.Catat(new RiwayatNotifikasi
                  {
                        SubscriberId = pelanggan.Id,
                        TitikPanasId = titik.Id,
                        Hasil = hasilKirim,
                        Keterangan = keterangan.Length > 300 ? keterangan[..300] : keterangan
                  });
            }

            private async Task CatatGagalAsync(string sumber, Exception ex)
            {
                  try
                  {
                        await _logRepo.CatatSinkronAsync(sumber, false, 0, ex.Message);
                  }
                  catch (Exception)
                  {
                        // Database sendiri yang bermasalah: tidak ada tempat mencatat,
                        // kesalahannya tetap sampai ke pemanggil lewat HasilSinkron.
                  }
            }
      }
}
