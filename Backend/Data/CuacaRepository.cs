using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    public class CuacaRepository : Repository<DataCuaca>
    {
        private const string KolomSelect =
            @"SELECT c.id, c.suhu, c.kelembapan, c.kecepatan_angin, c.arah_angin, c.waktu, c.sumber, c.wilayah_id
              FROM data_cuaca c";

        public CuacaRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "data_cuaca";

        protected override string QueryAmbilSemua => KolomSelect + " ORDER BY c.waktu DESC";

        protected override DataCuaca Petakan(NpgsqlDataReader rd)
        {
            return new DataCuaca
            {
                Id = rd.GetInt32(0),
                Suhu = rd.GetDouble(1),
                Kelembapan = rd.GetDouble(2),
                KecepatanAngin = rd.GetDouble(3),
                ArahAngin = rd.GetDouble(4),
                Waktu = rd.GetDateTime(5),
                Sumber = rd.GetString(6),
                WilayahId = rd.GetInt32(7)
            };
        }

        // POLYMORPHISM (override): yang dibutuhkan aplikasi adalah cuaca TERKINI,
        // jadi "ambil semua" di sini berarti satu baris terbaru per wilayah.
        public override Task<List<DataCuaca>> AmbilSemuaAsync()
        {
            return JalankanQueryAsync(
                @"SELECT DISTINCT ON (c.wilayah_id)
                         c.id, c.suhu, c.kelembapan, c.kecepatan_angin, c.arah_angin, c.waktu, c.sumber, c.wilayah_id
                  FROM data_cuaca c
                  ORDER BY c.wilayah_id, c.waktu DESC");
        }

        public async Task<int> SimpanBanyakAsync(IReadOnlyList<DataCuaca> daftar)
        {
            if (daftar.Count == 0)
                return 0;

            await using var conn = await Db.BukaKoneksiAsync();
            await using var batch = new NpgsqlBatch(conn);
            foreach (var c in daftar)
            {
                var cmd = new NpgsqlBatchCommand(
                    @"INSERT INTO data_cuaca (suhu, kelembapan, kecepatan_angin, arah_angin, waktu, sumber, wilayah_id)
                      VALUES (@suhu, @kelembapan, @angin, @arah, @waktu, @sumber, @wilayah)");
                cmd.Parameters.AddWithValue("suhu", c.Suhu);
                cmd.Parameters.AddWithValue("kelembapan", c.Kelembapan);
                cmd.Parameters.AddWithValue("angin", c.KecepatanAngin);
                cmd.Parameters.AddWithValue("arah", c.ArahAngin);
                cmd.Parameters.AddWithValue("waktu", DateTime.SpecifyKind(c.Waktu, DateTimeKind.Unspecified));
                cmd.Parameters.AddWithValue("sumber", c.Sumber);
                cmd.Parameters.AddWithValue("wilayah", c.WilayahId);
                batch.BatchCommands.Add(cmd);
            }
            return await batch.ExecuteNonQueryAsync();
        }
    }
}
