using System;

namespace SiagaApi.Backend.Models
{
      public class RingkasanProvinsi
      {
            public int Id { get; set; }
            public int JumlahHotspot { get; set; }
            public StatusSiaga StatusDominan { get; set; }
            public DateTime Waktu { get; set; }

            public int WilayahId { get; set; }
            public Wilayah? Wilayah { get; set; }
      }
}