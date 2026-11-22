using System;
using System.Security.Cryptography;

namespace SiagaApi.Backend.Services
{
    // Hash kata sandi dengan PBKDF2-HMAC-SHA256 (bawaan .NET, tanpa paket tambahan).
    // Format yang disimpan di kolom password_hash:
    //   pbkdf2-sha256$<iterasi>$<salt base64>$<hash base64>
    // Iterasi ikut disimpan supaya bisa dinaikkan nanti tanpa merusak akun lama.
    public static class KataSandi
    {
        private const string Skema = "pbkdf2-sha256";
        private const int Iterasi = 600_000;   // rekomendasi OWASP untuk PBKDF2-SHA256
        private const int PanjangSalt = 16;
        private const int PanjangHash = 32;

        public const int PanjangMinimal = 8;
        public const int PanjangMaksimal = 128;

        public static string Hash(string kataSandi)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(PanjangSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(kataSandi, salt, Iterasi, HashAlgorithmName.SHA256, PanjangHash);
            return $"{Skema}${Iterasi}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Cocok(string kataSandi, string tersimpan)
        {
            string[] bagian = tersimpan.Split('$');
            if (bagian.Length != 4 || bagian[0] != Skema || !int.TryParse(bagian[1], out int iterasi) || iterasi <= 0)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(bagian[2]);
                byte[] harapan = Convert.FromBase64String(bagian[3]);
                byte[] hitung = Rfc2898DeriveBytes.Pbkdf2(kataSandi, salt, iterasi, HashAlgorithmName.SHA256, harapan.Length);

                // Perbandingan waktu-tetap supaya lama pengecekan tidak membocorkan isi hash.
                return CryptographicOperations.FixedTimeEquals(hitung, harapan);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // Dipakai saat username tidak ditemukan: tetap menghitung satu hash supaya
        // lama respons "username salah" dan "kata sandi salah" tidak bisa dibedakan.
        public static void HitungPalsu(string kataSandi)
        {
            Rfc2898DeriveBytes.Pbkdf2(kataSandi, new byte[PanjangSalt], Iterasi, HashAlgorithmName.SHA256, PanjangHash);
        }
    }
}
