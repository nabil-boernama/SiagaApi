using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    public class TitikPanasRepository : Repository<TitikPanas>
    {
        private const string KolomSelect =
            @"SELECT t.id, t.lat, t.lon, t.confidence, t.brightness, t.satelit,
                     t.waktu_deteksi, t.wilayah_id
              FROM titik_panas t";

        public TitikPanasRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "titik_panas";

        protected override string QueryAmbilSemua =>
            KolomSelect + " ORDER BY t.waktu_deteksi DESC";

        protected override TitikPanas Petakan(NpgsqlDataReader rd)
        {
            return new TitikPanas
            {
                Id = rd.GetInt32(0),
                Lat = rd.GetDouble(1),
                Lon = rd.GetDouble(2),
                Confidence = rd.GetString(3),
                Brightness = rd.GetDouble(4),
                Satelit = rd.GetString(5),
                WaktuDeteksi = rd.GetDateTime(6),
                WilayahId = rd.GetInt32(7)
            };
        }

        // Titik panas di sebuah provinsi, termasuk yang tercatat di kabupaten-kabupatennya.
        public Task<List<TitikPanas>> AmbilPerProvinsiAsync(string provinsi)
        {
            return JalankanQueryAsync(
                KolomSelect + @"
                  JOIN wilayah w ON w.id = t.wilayah_id
                  WHERE w.provinsi = @provinsi
                  ORDER BY t.waktu_deteksi DESC",
                new NpgsqlParameter("provinsi", provinsi));
        }

        // Menyimpan hasil FirmsApiService.AmbilData() ke database.
        // Satu transaksi: kalau satu baris gagal, semua dibatalkan.
        public async Task<int> SimpanBanyakAsync(IEnumerable<TitikPanas> daftar)
        {
            const string sql =
                @"INSERT INTO titik_panas (lat, lon, confidence, brightness, satelit, waktu_deteksi, wilayah_id)
                  VALUES (@lat, @lon, @confidence, @brightness, @satelit, @waktu, @wilayah_id)
                  RETURNING id";

            int jumlah = 0;
            await using var conn = await Db.BukaKoneksiAsync();
            await using var trx = await conn.BeginTransactionAsync();
            foreach (var t in daftar)
            {
                await using var cmd = new NpgsqlCommand(sql, conn, trx);
                cmd.Parameters.AddWithValue("lat", t.Lat);
                cmd.Parameters.AddWithValue("lon", t.Lon);
                cmd.Parameters.AddWithValue("confidence", t.Confidence);
                cmd.Parameters.AddWithValue("brightness", t.Brightness);
                cmd.Parameters.AddWithValue("satelit", t.Satelit);
                cmd.Parameters.AddWithValue("waktu", t.WaktuDeteksi);
                cmd.Parameters.AddWithValue("wilayah_id", t.WilayahId);
                t.Id = System.Convert.ToInt32(await cmd.ExecuteScalarAsync());
                jumlah++;
            }
            await trx.CommitAsync();
            return jumlah;
        }
    }
}
