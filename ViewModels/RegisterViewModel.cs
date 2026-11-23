using System;
using System.Threading.Tasks;
using Avalonia.Media;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

public class RegisterViewModel : ViewModelBase
{
    private static readonly IBrush WarnaSukses = new SolidColorBrush(Color.Parse("#78C99A"));
    private static readonly IBrush WarnaGagal = new SolidColorBrush(Color.Parse("#E5796B"));

    private readonly Action _navigateToLogin;
    private readonly AutentikasiService _auth;

    private string _message = "";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private IBrush _messageForeground = WarnaGagal;

    public IBrush MessageForeground
    {
        get => _messageForeground;
        set => SetProperty(ref _messageForeground, value);
    }

    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public RegisterViewModel(Action navigateToLogin, AutentikasiService auth)
    {
        _navigateToLogin = navigateToLogin;
        _auth = auth;
    }

    // Simpan akun baru ke database. Mengembalikan true kalau pendaftaran tersimpan.
    public async Task<bool> DaftarAsync(
        string namaLengkap, string username, string email,
        string kataSandi, string konfirmasi, string kodeUndangan)
    {
        if (IsBusy)
            return false;

        IsBusy = true;
        Message = "";
        try
        {
            var hasil = await _auth.DaftarAsync(namaLengkap, username, email, kataSandi, konfirmasi, kodeUndangan);
            MessageForeground = hasil.Berhasil ? WarnaSukses : WarnaGagal;
            Message = hasil.Pesan;
            return hasil.Berhasil;
        }
        catch (Exception ex)
        {
            MessageForeground = WarnaGagal;
            Message = $"Tidak bisa terhubung ke database: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void NavigateToLogin()
    {
        _navigateToLogin();
    }
}
