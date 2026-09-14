using System;

namespace SiagaApi.Backend.Models
{
      public class DataCuaca
      {
            public int Id { get; set; }
            public double Suhu { get; set; }
            public double Kelembapan { get; set; }
            public double KecepatanAngin { get; set; }
            public double ArahAngin { get; set; }
            public DateTime Waktu { get; set; }

            public int WilayahId { get; set; }
            public Wilayah? Wilayah { get; set; }
      }
}