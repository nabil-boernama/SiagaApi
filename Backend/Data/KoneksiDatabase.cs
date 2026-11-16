using System;
using System.Threading.Tasks;
using Npgsql;

namespace SiagaApi.Backend.Data
{
    // ENCAPSULATION
    // Connection string disimpan private dan tidak pernah diekspos.
    // Kelas lain hanya bisa meminta koneksi yang sudah terbuka lewat
    // BukaKoneksiAsync(), tanpa tahu host, user, atau password-nya.
    public class KoneksiDatabase
    {
        // Nama environment variable untuk connection string asli.
        // Isi di terminal masing-masing, JANGAN di-commit ke repo:
        //   export SIAGAAPI_DB="Host=localhost;Port=5432;Username=postgres;Password=...;Database=siagaapi"
        public const string NamaEnvVar = "SIAGAAPI_DB";

        private const string DefaultLokal =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=siagaapi";

        private readonly string _connectionString;

        public KoneksiDatabase()
            : this(Environment.GetEnvironmentVariable(NamaEnvVar) ?? DefaultLokal)
        {
        }

        public KoneksiDatabase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string tidak boleh kosong.", nameof(connectionString));

            _connectionString = connectionString;
        }

        public async Task<NpgsqlConnection> BukaKoneksiAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        // Dipakai untuk menampilkan status koneksi di UI tanpa membocorkan password.
        public async Task<bool> CekKoneksiAsync()
        {
            try
            {
                await using var conn = await BukaKoneksiAsync();
                return true;
            }
            catch (NpgsqlException)
            {
                return false;
            }
        }
    }
}
