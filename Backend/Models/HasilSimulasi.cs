using System;

namespace SiagaApi.Backend.Models
{
      public class HasilSimulasi
      {
            public int Id { get; set; }
            public DateTime WaktuDijalankan { get; set; }
            public double ParameterAngin { get; set; }
            public double ParameterKelembapan { get; set; }

            public string SnapshotGrid { get; set; } = string.Empty;

            public int WilayahId { get; set; }
            public Wilayah? Wilayah { get; set; }
      }
}