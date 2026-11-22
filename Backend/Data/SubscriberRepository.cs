using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using SiagaApi.Backend.Models;

namespace SiagaApi.Backend.Data
{
    public class SubscriberRepository : Repository<Subscriber>
    {
        private const string KolomSelect =
            "SELECT id, email, status, terdaftar_pada, wilayah_id FROM subscriber";

        public SubscriberRepository(KoneksiDatabase db) : base(db)
        {
        }

        protected override string NamaTabel => "subscriber";

        protected override string QueryAmbilSemua => KolomSelect + " ORDER BY terdaftar_pada DESC, id DESC";

        protected override Subscriber Petakan(NpgsqlDataReader rd)
        {
            return new Subscriber
            {
                Id = rd.GetInt32(0),
                Email = rd.GetString(1),
                Status = Enum.Parse<StatusLangganan>(rd.GetString(2)),
                TerdaftarPada = rd.GetDateTime(3),
                WilayahId = rd.GetInt32(4)
            };
        }

        // Pelanggan aktif di satu wilayah: penerima peringatan email.
        public Task<List<Subscriber>> AmbilAktifPerWilayahAsync(int wilayahId)
        {
            return JalankanQueryAsync(
                KolomSelect + " WHERE wilayah_id = @wilayah AND status = 'Aktif' ORDER BY id",
                new NpgsqlParameter("wilayah", wilayahId));
        }

        // CREATE. Mengembalikan null kalau email itu sudah berlangganan di wilayah yang sama.
        public async Task<Subscriber?> TambahAsync(string email, int wilayahId)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO subscriber (email, wilayah_id) VALUES (@email, @wilayah)
                  ON CONFLICT (LOWER(email), wilayah_id) DO NOTHING
                  RETURNING id, email, status, terdaftar_pada, wilayah_id", conn);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("wilayah", wilayahId);
            await using var rd = await cmd.ExecuteReaderAsync();
            return await rd.ReadAsync() ? Petakan(rd) : null;
        }

        // UPDATE: jeda / lanjutkan langganan.
        public async Task<bool> UbahStatusAsync(int id, StatusLangganan status)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                "UPDATE subscriber SET status = @status WHERE id = @id AND status <> @status", conn);
            cmd.Parameters.AddWithValue("status", status.ToString());
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        // DELETE: berhenti berlangganan. Riwayat notifikasinya ikut terhapus (ON DELETE CASCADE).
        public async Task<bool> HapusAsync(int id)
        {
            await using var conn = await Db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand("DELETE FROM subscriber WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }
    }
}
