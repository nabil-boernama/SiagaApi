using System;

namespace SiagaApi.Backend.Models
{
      public class LaporanWarga
      {
            private const double RadiusPencocokanKm = 5.0;

            public int Id { get; set; }
            public string Pelapor { get; set; } = string.Empty;
            public string Deskripsi { get; set; } = string.Empty;
            public Keparahan Tingkat { get; set; }
            public StatusLaporan Status { get; set; }
            public DateTime DibuatPada { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }

            public int WilayahId { get; set; }
            public Wilayah? Wilayah { get; set; }

            public bool Verifikasi(TitikPanas titikPanas)
            {
                  double jarakKm = HitungJarakHaversineKm(Lat, Lon, titikPanas.Lat, titikPanas.Lon);
                  bool cocok = jarakKm <= RadiusPencocokanKm;

                  if (cocok)
                  {
                  Status = StatusLaporan.Diverifikasi;
                  }

                  return cocok;
            }

            private static double HitungJarakHaversineKm(double lat1, double lon1, double lat2, double lon2)
            {
                  const double radiusBumiKm = 6371.0;
                  double dLat = DegreeKeRadian(lat2 - lat1);
                  double dLon = DegreeKeRadian(lon2 - lon1);

                  double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(DegreeKeRadian(lat1)) * Math.Cos(DegreeKeRadian(lat2)) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

                  double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                  return radiusBumiKm * c;
            }

            private static double DegreeKeRadian(double derajat) => derajat * Math.PI / 180.0;
      }
}