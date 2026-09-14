using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public class KalkulatorRisiko
      {
            public int Hitung(DataCuaca cuaca, int jumlahHotspot)
            {
                  double skorKelembapan = (100 - cuaca.Kelembapan) * 0.4;
                  double skorAngin = cuaca.KecepatanAngin * 0.6;
                  double skorHotspot = jumlahHotspot * 2.0;

                  double total = skorKelembapan + skorAngin + skorHotspot;
                  return (int)System.Math.Clamp(total, 0, 100);
            }

            public StatusSiaga TentukanStatus(int skor)
            {
                  if (skor >= 70) return StatusSiaga.Merah;
                  if (skor >= 40) return StatusSiaga.Kuning;
                  return StatusSiaga.Hijau;
            }
      }
}