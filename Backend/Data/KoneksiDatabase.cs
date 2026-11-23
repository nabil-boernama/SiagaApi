using System;
using System.IO;
using System.Text.Json;
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
        // Urutan sumber connection string (yang pertama ditemukan dipakai):
        //   1. environment variable SIAGAAPI_DB
        //   2. appsettings.json di folder yang sama dengan SiagaApi.exe,
        //      kunci "ConnectionStrings" -> "SiagaApi"
        //   3. PostgreSQL lokal default (localhost / postgres / postgres)
        // appsettings.json berisi password asli, JANGAN di-commit ke repo.
        // Contoh isinya ada di appsettings.example.json.
        public const string NamaEnvVar = "SIAGAAPI_DB";
        public const string NamaFileKonfigurasi = "appsettings.json";

        private const string DefaultLokal =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=siagaapi";

        private readonly string _connectionString;

        // Asal connection string ("environment variable SIAGAAPI_DB", "appsettings.json",
        // atau "default lokal"). Aman ditampilkan di UI karena tidak memuat password.
        public string SumberKonfigurasi { get; }

        public KoneksiDatabase()
        {
            (_connectionString, SumberKonfigurasi) = TentukanConnectionString();
        }

        public KoneksiDatabase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string tidak boleh kosong.", nameof(connectionString));

            _connectionString = connectionString;
            SumberKonfigurasi = "parameter konstruktor";
        }

        private static (string, string) TentukanConnectionString()
        {
            string? dariEnv = Environment.GetEnvironmentVariable(NamaEnvVar);
            if (!string.IsNullOrWhiteSpace(dariEnv))
                return (dariEnv, $"environment variable {NamaEnvVar}");

            string? dariFile = BacaDariFileKonfigurasi();
            if (!string.IsNullOrWhiteSpace(dariFile))
                return (dariFile, NamaFileKonfigurasi);

            return (DefaultLokal, "default lokal (localhost)");
        }

        private static string? BacaDariFileKonfigurasi()
        {
            // AppContext.BaseDirectory = folder SiagaApi.exe (juga untuk single-file publish).
            string path = Path.Combine(AppContext.BaseDirectory, NamaFileKonfigurasi);
            if (!File.Exists(path))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("SiagaApi", out var nilai) &&
                    nilai.ValueKind == JsonValueKind.String)
                {
                    return nilai.GetString();
                }
            }
            catch (JsonException)
            {
                // File rusak: abaikan dan jatuh ke default lokal. Kegagalan koneksi
                // nanti tetap terlihat di UI lewat SumberKonfigurasi.
            }

            return null;
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
