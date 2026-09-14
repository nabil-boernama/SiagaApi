using System;
using System.Collections.Generic;

namespace SiagaApi.Backend.Models
{
      public class TitikPanas
      {
            public int Id { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public string Confidence { get; set; } = string.Empty;
            public double Brightness { get; set; }
            public string Satelit { get; set; } = string.Empty;
            public DateTime WaktuDeteksi { get; set; }

            public int WilayahId { get; set; }
            public Wilayah? Wilayah { get; set; }

            public List<RiwayatNotifikasi> RiwayatNotifikasi { get; set; } = new();
      }
}