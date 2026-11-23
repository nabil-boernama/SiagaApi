using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public record LanggananItem(int Id, string Email, string NamaWilayah, DateTime TerdaftarUtc, StatusLangganan Status);

      public record HasilLangganan(bool Berhasil, string Pesan);

      // Langganan peringatan email warga: tambah, jeda, lanjutkan, hapus.
      public class LanggananService
      {
            private readonly WilayahRepository _wilayahRepo;
            private readonly SubscriberRepository _subscriberRepo;
            private readonly RiwayatNotifikasiRepository _riwayatRepo;

            public LanggananService(KoneksiDatabase db)
            {
                  _wilayahRepo = new WilayahRepository(db);
                  _subscriberRepo = new SubscriberRepository(db);
                  _riwayatRepo = new RiwayatNotifikasiRepository(db);
            }

            public async Task<List<Wilayah>> AmbilKabupatenAsync(string provinsi)
            {
                  return (await _wilayahRepo.AmbilSemuaAsync())
                        .Where(w => w.Tingkat == TingkatWilayah.Kabupaten && w.Provinsi == provinsi)
                        .OrderBy(w => w.Nama)
                        .ToList();
            }

            public async Task<List<LanggananItem>> AmbilLanggananAsync()
            {
                  var nama = (await _wilayahRepo.AmbilSemuaAsync()).ToDictionary(w => w.Id, w => w.Nama);
                  return (await _subscriberRepo.AmbilSemuaAsync())
                        .Select(s => new LanggananItem(s.Id, s.Email, nama.GetValueOrDefault(s.WilayahId, "-"), s.TerdaftarPada, s.Status))
                        .ToList();
            }

            public Task<List<CatatanNotifikasi>> AmbilNotifikasiAsync(int batas = 20)
            {
                  return _riwayatRepo.AmbilTerbaruAsync(batas);
            }

            public async Task<HasilLangganan> TambahAsync(string email, int wilayahId)
            {
                  email = (email ?? "").Trim().ToLowerInvariant();
                  if (email.Length == 0)
                        return new HasilLangganan(false, "Email warga wajib diisi.");
                  if (!EmailValid(email))
                        return new HasilLangganan(false, "Format email belum valid.");

                  var tersimpan = await _subscriberRepo.TambahAsync(email, wilayahId);
                  return tersimpan is null
                        ? new HasilLangganan(false, "Email tersebut sudah berlangganan di wilayah ini.")
                        : new HasilLangganan(true, "Langganan berhasil ditambahkan.");
            }

            public Task<bool> JedaAsync(int id) => _subscriberRepo.UbahStatusAsync(id, StatusLangganan.Dijeda);

            public Task<bool> LanjutkanAsync(int id) => _subscriberRepo.UbahStatusAsync(id, StatusLangganan.Aktif);

            public Task<bool> HapusAsync(int id) => _subscriberRepo.HapusAsync(id);

            private static bool EmailValid(string email)
            {
                  if (email.Length > 254 || email.Contains(' '))
                        return false;
                  if (!MailAddress.TryCreate(email, out var alamat) || alamat.Address != email)
                        return false;
                  return alamat.Host.Contains('.') && !alamat.Host.StartsWith('.') && !alamat.Host.EndsWith('.');
            }
      }
}
