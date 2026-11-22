using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    public class WilayahRepository : Repository<Wilayah>
    {
        public WilayahRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "wilayah";

        protected override string QueryAmbilSemua =>
            @"SELECT id, nama, tingkat, provinsi, lat, lon, wilayah_induk_id
              FROM wilayah
              ORDER BY provinsi, tingkat DESC, nama";

        protected override Wilayah Petakan(NpgsqlDataReader rd)
        {
            return new Wilayah
            {
                Id = rd.GetInt32(0),
                Nama = rd.GetString(1),
                Tingkat = Enum.Parse<TingkatWilayah>(rd.GetString(2)),
                Provinsi = rd.GetString(3),
                Lat = rd.GetDouble(4),
                Lon = rd.GetDouble(5),
                WilayahIndukId = rd.IsDBNull(6) ? null : rd.GetInt32(6)
            };
        }

        public Task<List<Wilayah>> AmbilProvinsiAsync()
        {
            return JalankanQueryAsync(
                @"SELECT id, nama, tingkat, provinsi, lat, lon, wilayah_induk_id
                  FROM wilayah
                  WHERE tingkat = 'Provinsi'
                  ORDER BY nama");
        }

        // Garis batas kabupaten/kota: id wilayah -> daftar poligon, tiap poligon
        // daftar titik (lat, lon). Dipisah dari AmbilSemuaAsync karena datanya
        // besar dan hanya dibutuhkan untuk peta dan penentuan wilayah hotspot.
        public async Task<Dictionary<int, List<List<(double Lat, double Lon)>>>> AmbilBatasAsync()
        {
            var hasil = new Dictionary<int, List<List<(double, double)>>>();
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand("SELECT id, batas FROM wilayah WHERE batas IS NOT NULL", conn);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                var poligon = new List<List<(double, double)>>();
                using var doc = System.Text.Json.JsonDocument.Parse(rd.GetString(1));
                foreach (var cincin in doc.RootElement.EnumerateArray())
                {
                    var titik = new List<(double, double)>();
                    foreach (var t in cincin.EnumerateArray())
                    {
                        titik.Add((t[0].GetDouble(), t[1].GetDouble()));
                    }
                    poligon.Add(titik);
                }
                hasil[rd.GetInt32(0)] = poligon;
            }
            return hasil;
        }
    }
}
