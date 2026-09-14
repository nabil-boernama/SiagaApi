using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public interface IRiwayatNotifikasiStore
      {
            Task<bool> SudahPernahDikirim(int subscriberId, int titikPanasId);
            Task Catat(RiwayatNotifikasi riwayat);
      }

      public class PengaturanSmtp
      {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; } = 587;
            public string EmailPengirim { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
      }

      public class NotifikasiService
      {
            private readonly IRiwayatNotifikasiStore _riwayatStore;
            private readonly PengaturanSmtp _smtp;

            public NotifikasiService(IRiwayatNotifikasiStore riwayatStore, PengaturanSmtp smtp)
            {
                  _riwayatStore = riwayatStore;
                  _smtp = smtp;
            }

            public async Task CekDanKirim(Wilayah wilayah, List<TitikPanas> titikPanasBaru)
            {
                  foreach (var subscriber in wilayah.Subscriber)
                  {
                  foreach (var titik in titikPanasBaru)
                  {
                        bool sudahDinotifikasi = await SudahDinotifikasi(subscriber, titik);
                        if (sudahDinotifikasi) continue;

                        await KirimEmail(subscriber, titik);

                        await _riwayatStore.Catat(new RiwayatNotifikasi
                        {
                              SubscriberId = subscriber.Id,
                              TitikPanasId = titik.Id,
                              WaktuDikirim = DateTime.UtcNow
                        });
                  }
                  }
            }

            private Task<bool> SudahDinotifikasi(Subscriber subscriber, TitikPanas titikPanas)
            {
                  return _riwayatStore.SudahPernahDikirim(subscriber.Id, titikPanas.Id);
            }

            private async Task KirimEmail(Subscriber subscriber, TitikPanas titikPanas)
            {
                  using var client = new SmtpClient(_smtp.Host, _smtp.Port)
                  {
                  Credentials = new NetworkCredential(_smtp.Username, _smtp.Password),
                  EnableSsl = true
                  };

                  var pesan = new MailMessage
                  {
                  From = new MailAddress(_smtp.EmailPengirim),
                  Subject = "Peringatan Dini: Terdeteksi Titik Panas di Wilayah Anda",
                  Body = $"Terdeteksi titik panas (hotspot) di sekitar wilayah Anda pada " +
                        $"{titikPanas.WaktuDeteksi:dd MMM yyyy HH:mm} WIB, dengan tingkat " +
                        $"keyakinan (confidence) '{titikPanas.Confidence}' dari satelit " +
                        $"{titikPanas.Satelit}. Ini adalah sinyal awal yang perlu diperiksa, " +
                        $"bukan kepastian adanya kebakaran. Mohon tetap waspada dan pantau " +
                        $"informasi lebih lanjut dari petugas setempat.",
                  IsBodyHtml = false
                  };
                  pesan.To.Add(subscriber.Email);

                  await client.SendMailAsync(pesan);
            }
      }
}