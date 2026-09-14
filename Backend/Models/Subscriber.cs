using System;
using System.Collections.Generic;

namespace SiagaApi.Backend.Models
{
      public class Subscriber
      {
            public int Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public DateTime TerdaftarPada { get; set; }

            public int WilayahId { get; set; }
            public Wilayah? Wilayah { get; set; }

            public List<RiwayatNotifikasi> RiwayatNotifikasi { get; set; } = new();
      }
}