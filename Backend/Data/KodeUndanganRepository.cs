using System.Security.Cryptography;
using System.Threading.Tasks;
using Npgsql;

namespace SiagaApi.Backend.Data
{
    // Kode undangan posko: hanya satu yang aktif pada satu waktu.
    public class KodeUndanganRepository
    {
        // Tanpa huruf/angka yang mirip (0/O, 1/I/L) supaya mudah dibacakan.
        private const string Alfabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        private readonly KoneksiDatabase _db;

        public KodeUndanganRepository(KoneksiDatabase db)
        {
            _db = db;
        }

        public async Task<string?> AmbilAktifAsync()
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT kode FROM kode_undangan WHERE aktif ORDER BY dibuat_pada DESC LIMIT 1", conn);
            return await cmd.ExecuteScalarAsync() as string;
        }

        // Kode lama dinonaktifkan dan kode baru dibuat dalam satu transaksi,
        // jadi tidak pernah ada dua kode aktif.
        public async Task<string> BuatBaruAsync(int adminId)
        {
            string kode = BuatKodeAcak();

            await using var conn = await _db.BukaKoneksiAsync();
            await using var tx = await conn.BeginTransactionAsync();

            await using (var nonaktif = new NpgsqlCommand("UPDATE kode_undangan SET aktif = FALSE WHERE aktif", conn, tx))
            {
                await nonaktif.ExecuteNonQueryAsync();
            }

            await using (var buat = new NpgsqlCommand(
                "INSERT INTO kode_undangan (kode, dibuat_oleh) VALUES (@kode, @admin)", conn, tx))
            {
                buat.Parameters.AddWithValue("kode", kode);
                buat.Parameters.AddWithValue("admin", adminId);
                await buat.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return kode;
        }

        // Format XXXX-XXXX dari pembangkit acak kriptografis.
        private static string BuatKodeAcak()
        {
            var huruf = new char[9];
            for (int i = 0; i < huruf.Length; i++)
            {
                huruf[i] = i == 4 ? '-' : Alfabet[RandomNumberGenerator.GetInt32(Alfabet.Length)];
            }
            return new string(huruf);
        }
    }
}
