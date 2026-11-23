using System.Threading.Tasks;
using Npgsql;

namespace SiagaApi.Backend.Data
{
    // Sesi "Ingat saya di komputer ini". Database hanya menyimpan hash token.
    public class SesiLoginRepository
    {
        private readonly KoneksiDatabase _db;

        public SesiLoginRepository(KoneksiDatabase db)
        {
            _db = db;
        }

        public async Task BuatAsync(int operatorId, string tokenHash, int masaBerlakuHari)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"WITH bersihkan AS (
                      DELETE FROM sesi_login WHERE kedaluwarsa_pada < (NOW() AT TIME ZONE 'UTC')
                  )
                  INSERT INTO sesi_login (operator_id, token_hash, kedaluwarsa_pada)
                  VALUES (@operator, @hash, (NOW() AT TIME ZONE 'UTC') + make_interval(days => @hari))", conn);
            cmd.Parameters.AddWithValue("operator", operatorId);
            cmd.Parameters.AddWithValue("hash", tokenHash);
            cmd.Parameters.AddWithValue("hari", masaBerlakuHari);
            await cmd.ExecuteNonQueryAsync();
        }

        // Id operator pemilik sesi, atau null kalau token tidak dikenal,
        // sudah kedaluwarsa, atau akunnya sudah tidak Aktif.
        public async Task<int?> CariOperatorIdAsync(string tokenHash)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand(
                @"SELECT s.operator_id
                  FROM sesi_login s
                  JOIN akun_operator a ON a.id = s.operator_id
                  WHERE s.token_hash = @hash
                    AND s.kedaluwarsa_pada > (NOW() AT TIME ZONE 'UTC')
                    AND a.status = 'Aktif'", conn);
            cmd.Parameters.AddWithValue("hash", tokenHash);
            return await cmd.ExecuteScalarAsync() as int?;
        }

        public async Task HapusAsync(string tokenHash)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand("DELETE FROM sesi_login WHERE token_hash = @hash", conn);
            cmd.Parameters.AddWithValue("hash", tokenHash);
            await cmd.ExecuteNonQueryAsync();
        }

        // Mencabut semua sesi satu akun, mis. setelah kata sandinya direset
        // atau akunnya dinonaktifkan.
        public async Task HapusSemuaMilikAsync(int operatorId)
        {
            await using var conn = await _db.BukaKoneksiAsync();
            await using var cmd = new NpgsqlCommand("DELETE FROM sesi_login WHERE operator_id = @operator", conn);
            cmd.Parameters.AddWithValue("operator", operatorId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
