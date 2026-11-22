using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    public class IndeksRisikoRepository : Repository<IndeksRisiko>
    {
        public IndeksRisikoRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "indeks_risiko";

        // Satu indeks terbaru per wilayah.
        protected override string QueryAmbilSemua =>
            @"SELECT DISTINCT ON (wilayah_id) id, skor, status, waktu, wilayah_id
              FROM indeks_risiko
              ORDER BY wilayah_id, waktu DESC";

        protected override IndeksRisiko Petakan(NpgsqlDataReader rd)
        {
            return new IndeksRisiko
            {
                Id = rd.GetInt32(0),
                Skor = rd.GetInt32(1),
                Status = Enum.Parse<StatusSiaga>(rd.GetString(2)),
                Waktu = rd.GetDateTime(3),
                WilayahId = rd.GetInt32(4)
            };
        }

        public async Task<int> SimpanBanyakAsync(IReadOnlyList<IndeksRisiko> daftar)
        {
            if (daftar.Count == 0)
                return 0;

            await using var conn = await Db.BukaKoneksiAsync();
            await using var batch = new NpgsqlBatch(conn);
            foreach (var r in daftar)
            {
                var cmd = new NpgsqlBatchCommand(
                    "INSERT INTO indeks_risiko (skor, status, wilayah_id) VALUES (@skor, @status, @wilayah)");
                cmd.Parameters.AddWithValue("skor", r.Skor);
                cmd.Parameters.AddWithValue("status", r.Status.ToString());
                cmd.Parameters.AddWithValue("wilayah", r.WilayahId);
                batch.BatchCommands.Add(cmd);
            }
            return await batch.ExecuteNonQueryAsync();
        }
    }
}
