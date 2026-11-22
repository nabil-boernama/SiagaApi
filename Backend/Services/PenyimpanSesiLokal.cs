using System;
using System.IO;

namespace SiagaApi.Backend.Services
{
    // Menyimpan token "Ingat saya" di folder data pengguna komputer ini
    // (Windows: %APPDATA%\SiagaApi, macOS/Linux: ~/.config/SiagaApi).
    // Yang disimpan token acak, bukan kata sandi. Token bisa dicabut dari
    // database (tabel sesi_login) dan kedaluwarsa sendiri.
    public class PenyimpanSesiLokal
    {
        private readonly string _path;

        public PenyimpanSesiLokal()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SiagaApi", "sesi.token"))
        {
        }

        public PenyimpanSesiLokal(string path)
        {
            _path = path;
        }

        public string? Baca()
        {
            try
            {
                if (!File.Exists(_path))
                    return null;

                string token = File.ReadAllText(_path).Trim();
                return token.Length == 0 ? null : token;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        public void Simpan(string token)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, token);

            // Di macOS/Linux, batasi supaya hanya pemilik akun komputer yang bisa membaca.
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(_path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        public void Hapus()
        {
            try
            {
                File.Delete(_path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
