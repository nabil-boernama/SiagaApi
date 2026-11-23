using System;
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

        // Titik panas yang terdeteksi sejak waktu tertentu (UTC), semua wilayah.
        public Task<List<TitikPanas>> AmbilSejakAsync(DateTime sejakUtc)
        {
            return JalankanQueryAsync(
                KolomSelect + " WHERE t.waktu_deteksi >= @sejak ORDER BY t.waktu_deteksi DESC",
                new NpgsqlParameter("sejak", DateTime.SpecifyKind(sejakUtc, DateTimeKind.Unspecified)));
        }

        // CREATE untuk sinkronisasi: menyimpan hanya deteksi yang BELUM ada
        // (indeks unik uq_titik_panas_deteksi + ON CONFLICT DO NOTHING), jadi aman
        // dijalankan berulang. Mengembalikan baris yang benar-benar baru, Id terisi.
        public async Task<List<TitikPanas>> SimpanBaruAsync(IReadOnlyList<TitikPanas> daftar)
        {
            var baru = new List<TitikPanas>();
            if (daftar.Count == 0)
                return baru;

            const string sql =
                @"INSERT INTO titik_panas (lat, lon, confidence, brightness, satelit, waktu_deteksi, wilayah_id)
                  VALUES (@lat, @lon, @confidence, @brightness, @satelit, @waktu, @wilayah_id)
                  ON CONFLICT (lat, lon, waktu_deteksi, satelit) DO NOTHING
                  RETURNING id";

            await using var conn = await Db.BukaKoneksiAsync();
            await using var trx = await conn.BeginTransactionAsync();
            await using var batch = new NpgsqlBatch(conn, trx);
            foreach (var t in daftar)
            {
                var cmd = new NpgsqlBatchCommand(sql);
                cmd.Parameters.AddWithValue("lat", t.Lat);
                cmd.Parameters.AddWithValue("lon", t.Lon);
                cmd.Parameters.AddWithValue("confidence", t.Confidence);
                cmd.Parameters.AddWithValue("brightness", t.Brightness);
                cmd.Parameters.AddWithValue("satelit", t.Satelit);
                cmd.Parameters.AddWithValue("waktu", DateTime.SpecifyKind(t.WaktuDeteksi, DateTimeKind.Unspecified));
                cmd.Parameters.AddWithValue("wilayah_id", t.WilayahId);
                batch.BatchCommands.Add(cmd);
            }

            // Satu hasil per perintah, berurutan: ada baris = tersimpan, kosong = sudah ada.
            await using (var rd = await batch.ExecuteReaderAsync())
            {
                foreach (var t in daftar)
                {
                    if (await rd.ReadAsync())
                    {
                        t.Id = rd.GetInt32(0);
                        baru.Add(t);
                    }
                    await rd.NextResultAsync();
                }
            }

            await trx.CommitAsync();
            return baru;
        }

        // Jumlah deteksi per hari (tanggal WIB) sejak waktu tertentu, untuk
        // satu provinsi atau satu kabupaten (wilayahId).
        public async Task<Dictionary<DateOnly, int>> HitungPerHariAsync(DateTime sejakUtc, string provinsi, int? wilayahId)
        {
            const string sql =
                @"SELECT (t.waktu_deteksi + INTERVAL '7 hours')::date, COUNT(*)
                  FROM titik_panas t
                  JOIN wilayah w ON w.id = t.wilayah_id
                  WHERE t.waktu_deteksi >= @sejak
                    AND w.provinsi = @provinsi
                    AND (@wilayah::int IS NULL OR t.wilayah_id = @wilayah)
                  GROUP BY 1";

            var hasil = new Dictionary<DateOnly, int>();
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("sejak", DateTime.SpecifyKind(sejakUtc, DateTimeKind.Unspecified));
            cmd.Parameters.AddWithValue("provinsi", provinsi);
            cmd.Parameters.Add(new NpgsqlParameter("wilayah", NpgsqlTypes.NpgsqlDbType.Integer) { Value = (object?)wilayahId ?? DBNull.Value });
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                hasil[DateOnly.FromDateTime(rd.GetDateTime(0))] = Convert.ToInt32(rd.GetInt64(1));
            }
            return hasil;
        }
    }
}
