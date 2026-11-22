using SiagaApi.Backend.Models;

namespace SiagaApi.ViewModels;

// Operator yang sedang masuk di aplikasi ini. Diisi saat login berhasil
// dan dikosongkan saat keluar.
public static class SesiAplikasi
{
    public static AkunOperator? Operator { get; private set; }

    // Token "Ingat saya" milik sesi ini (null kalau tidak dicentang).
    public static string? Token { get; private set; }

    public static bool SudahMasuk => Operator is not null;

    public static void Mulai(AkunOperator akun, string? token)
    {
        Operator = akun;
        Token = token;
    }

    public static void Akhiri()
    {
        Operator = null;
        Token = null;
    }
}
