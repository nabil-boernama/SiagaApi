using System;

namespace SiagaApi.Backend.Models
{
      public class IndeksRisiko
      {
            public int Id { get; set; }
            public int Skor { get; set; }
            public StatusSiaga Status { get; set; }
            public DateTime Waktu { get; set; }

            public int WilayahId { get; set; }
            public Wilayah? Wilayah { get; set; }
      }
}