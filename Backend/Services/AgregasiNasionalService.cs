using System;
using System.Threading.Tasks;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Services
{
      public interface IAgregatHotspotQuery
      {
            Task<int> HitungJumlahHotspot(int wilayahProvinsiId);
            Task<StatusSiaga> AmbilStatusDominan(int wilayahProvinsiId);
      }

      public class AgregasiNasionalService
      {
            private readonly IAgregatHotspotQuery _query;

            public AgregasiNasionalService(IAgregatHotspotQuery query)
            {
                  _query = query;
            }

            public async Task<RingkasanProvinsi> HitungRingkasan(Wilayah provinsi)
            {
                  if (provinsi.Tingkat != TingkatWilayah.Provinsi)
                  throw new ArgumentException("HitungRingkasan hanya berlaku untuk Wilayah bertingkat Provinsi.", nameof(provinsi));

                  int jumlahHotspot = await _query.HitungJumlahHotspot(provinsi.Id);
                  StatusSiaga statusDominan = await _query.AmbilStatusDominan(provinsi.Id);

                  return new RingkasanProvinsi
                  {
                  WilayahId = provinsi.Id,
                  JumlahHotspot = jumlahHotspot,
                  StatusDominan = statusDominan,
                  Waktu = DateTime.UtcNow
                  };
            }
      }
}