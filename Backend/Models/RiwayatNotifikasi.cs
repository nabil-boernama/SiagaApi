using System;

namespace SiagaApi.Backend.Models
{
      public class RiwayatNotifikasi
      {
            public int Id { get; set; }
            public DateTime WaktuDikirim { get; set; }
            public HasilNotifikasi Hasil { get; set; } = HasilNotifikasi.Terkirim;
            public string Keterangan { get; set; } = string.Empty;

            public int SubscriberId { get; set; }
            public Subscriber? Subscriber { get; set; }

            public int TitikPanasId { get; set; }
            public TitikPanas? TitikPanas { get; set; }
      }
}