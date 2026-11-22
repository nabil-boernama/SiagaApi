using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;

namespace SiagaApi.Backend.Services
{
      // Pengaturan posko yang dipakai bersama semua operator (disimpan di database).
      public class PengaturanPosko
      {
            public const string TanpaPetaDasar = "Tanpa peta dasar";
            public const string PetaJalan = "Peta jalan";
            public const string PetaSatelit = "Satelit";

            public string Provinsi { get; set; } = "Riau";
            public bool ModeNasional { get; set; } = true;
            public string PetaDasar { get; set; } = TanpaPetaDasar;
            public int IntervalMenit { get; set; } = 30;
      }

      // Pengaturan yang HANYA disimpan di komputer ini: kunci API dan data login
      // SMTP. Sengaja tidak masuk database, karena semua salinan aplikasi memakai
      // akun database yang sama dan bisa membaca isinya.
      public class PengaturanLokal
      {
            public string FirmsMapKey { get; set; } = string.Empty;
            public string AccuWeatherKey { get; set; } = string.Empty;
            public string SmtpHost { get; set; } = string.Empty;
            public int SmtpPort { get; set; } = 587;
            public string SmtpEmailPengirim { get; set; } = string.Empty;
            public string SmtpUsername { get; set; } = string.Empty;
            public string SmtpPassword { get; set; } = string.Empty;

            public bool SmtpLengkap =>
                  !string.IsNullOrWhiteSpace(SmtpHost) && !string.IsNullOrWhiteSpace(SmtpEmailPengirim) &&
                  !string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword);
      }

      public class PengaturanService
      {
            private readonly PengaturanRepository _repo;
            private readonly AkunOperatorRepository _akunRepo;
            private readonly string _pathLokal;

            public PengaturanService(KoneksiDatabase db)
                  : this(db, Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SiagaApi", "pengaturan-lokal.json"))
            {
            }

            public PengaturanService(KoneksiDatabase db, string pathLokal)
            {
                  _repo = new PengaturanRepository(db);
                  _akunRepo = new AkunOperatorRepository(db);
                  _pathLokal = pathLokal;
            }

            public async Task<PengaturanPosko> MuatPoskoAsync()
            {
                  var nilai = await _repo.AmbilSemuaAsync();
                  var p = new PengaturanPosko();
                  if (nilai.TryGetValue("provinsi", out var provinsi) && provinsi.Length > 0) p.Provinsi = provinsi;
                  if (nilai.TryGetValue("mode_nasional", out var nasional)) p.ModeNasional = nasional == "true";
                  if (nilai.TryGetValue("peta_dasar", out var peta) && peta.Length > 0) p.PetaDasar = peta;
                  if (nilai.TryGetValue("interval_menit", out var interval) &&
                      int.TryParse(interval, NumberStyles.Integer, CultureInfo.InvariantCulture, out int menit) && menit >= 5)
                        p.IntervalMenit = menit;
                  return p;
            }

            // Hanya Admin dan SuperAdmin yang boleh mengubah pengaturan posko.
            // Peran dicek ulang ke database.
            public async Task SimpanPoskoAsync(PengaturanPosko p, int pelakuId)
            {
                  var pelaku = await _akunRepo.CariPerIdAsync(pelakuId);
                  if (pelaku is null || !pelaku.Pengelola || pelaku.Status != Models.StatusAkun.Aktif)
                        throw new UnauthorizedAccessException("Hanya admin posko yang boleh mengubah pengaturan.");

                  await _repo.SimpanAsync(new Dictionary<string, string>
                  {
                        ["provinsi"] = p.Provinsi,
                        ["mode_nasional"] = p.ModeNasional ? "true" : "false",
                        ["peta_dasar"] = p.PetaDasar,
                        ["interval_menit"] = p.IntervalMenit.ToString(CultureInfo.InvariantCulture)
                  }, pelakuId);
            }

            public PengaturanLokal MuatLokal()
            {
                  try
                  {
                        if (File.Exists(_pathLokal))
                              return JsonSerializer.Deserialize<PengaturanLokal>(File.ReadAllText(_pathLokal)) ?? new PengaturanLokal();
                  }
                  catch (JsonException)
                  {
                        // File rusak: pakai nilai kosong, operator bisa mengisi ulang.
                  }
                  catch (IOException)
                  {
                  }
                  return new PengaturanLokal();
            }

            public void SimpanLokal(PengaturanLokal lokal)
            {
                  Directory.CreateDirectory(Path.GetDirectoryName(_pathLokal)!);
                  File.WriteAllText(_pathLokal, JsonSerializer.Serialize(lokal, new JsonSerializerOptions { WriteIndented = true }));

                  // Berisi kata sandi SMTP: di macOS/Linux hanya pemilik akun komputer yang boleh membaca.
                  if (!OperatingSystem.IsWindows())
                        File.SetUnixFileMode(_pathLokal, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
      }
}
