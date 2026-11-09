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
    }
}
