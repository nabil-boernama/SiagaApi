using System.Collections.Generic;

namespace SiagaApi.Backend.Models
{
      public class Wilayah
      {
            public int Id { get; set; }
            public string Nama { get; set; } = string.Empty;
            public TingkatWilayah Tingkat { get; set; }
            public string Provinsi { get; set; } = string.Empty;
            public double Lat { get; set; }
            public double Lon { get; set; }

            public int? WilayahIndukId { get; set; }
            public Wilayah? WilayahInduk { get; set; }
            public List<Wilayah> AnakWilayah { get; set; } = new();

            public List<TitikPanas> TitikPanas { get; set; } = new();
            public List<DataCuaca> DataCuaca { get; set; } = new();
            public List<LaporanWarga> LaporanWarga { get; set; } = new();
            public List<IndeksRisiko> IndeksRisiko { get; set; } = new();
            public List<Subscriber> Subscriber { get; set; } = new();
            public List<RingkasanProvinsi> RingkasanProvinsi { get; set; } = new();
      }
}