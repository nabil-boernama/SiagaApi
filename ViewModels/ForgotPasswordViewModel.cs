using System;

namespace SiagaApi.ViewModels;

// Halaman "Lupa kata sandi". Aplikasi tidak mengirim kata sandi lewat email:
// pemulihan dilakukan admin posko dari panel akun (AutentikasiService.ResetKataSandiAsync),
// jadi halaman ini hanya menjelaskan langkahnya.
public class ForgotPasswordViewModel : ViewModelBase
{
    private readonly Action _navigateToLogin;

    public LangkahPemulihan[] Langkah { get; } =
    {
        new("1", "Hubungi admin posko dan sebutkan username Anda."),
        new("2", "Admin membuka Pengaturan, tab Akun operator, lalu menekan Reset sandi pada akun Anda."),
        new("3", "Masuk dengan kata sandi sementara yang diberikan admin."),
        new("4", "Ganti kata sandi di Pengaturan, tab Keamanan.")
    };

    public string StatusMessage =>
        "Lima kali salah kata sandi mengunci akun selama 5 menit. Reset oleh admin juga membuka kunci itu.";

    public ForgotPasswordViewModel(Action navigateToLogin)
    {
        _navigateToLogin = navigateToLogin;
    }

    public void NavigateToLogin()
    {
        _navigateToLogin();
    }
}

public record LangkahPemulihan(string Nomor, string Teks);
