using System;
using System.Threading.Tasks;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly Action _navigateToRegister;
    private readonly Action _navigateToDashboard;
    private readonly Action _navigateToForgotPassword;
    private readonly AutentikasiService _auth;
    private readonly PenyimpanSesiLokal _sesiLokal;

    private string _message = "";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    // Hasil pengecekan koneksi database, tampil di bawah formulir.
    private string _databaseStatus = "Memeriksa database...";

    public string DatabaseStatus
    {
        get => _databaseStatus;
        set => SetProperty(ref _databaseStatus, value);
    }

    private Avalonia.Media.IBrush _databaseBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#66737E"));

    public Avalonia.Media.IBrush DatabaseBrush
    {
        get => _databaseBrush;
        set => SetProperty(ref _databaseBrush, value);
    }

    // Status hanya ditampilkan kalau database tidak terjangkau; saat normal tidak perlu tampil.
    private bool _databaseBermasalah;

    public bool DatabaseBermasalah
    {
        get => _databaseBermasalah;
        set => SetProperty(ref _databaseBermasalah, value);
    }

    public void AturStatusDatabase(bool terhubung, string sumberKonfigurasi)
    {
        DatabaseBermasalah = !terhubung;
        DatabaseStatus = terhubung ? "Database terhubung" : $"Database tidak terjangkau ({sumberKonfigurasi})";
        DatabaseBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(terhubung ? "#5CC98A" : "#F2A83B"));
    }

    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public LoginViewModel(
        Action navigateToRegister,
        Action navigateToDashboard,
        Action navigateToForgotPassword,
        AutentikasiService auth,
        PenyimpanSesiLokal sesiLokal)
    {
        _navigateToRegister = navigateToRegister;
        _navigateToDashboard = navigateToDashboard;
        _navigateToForgotPassword = navigateToForgotPassword;
        _auth = auth;
        _sesiLokal = sesiLokal;
    }

    // Cek nama pengguna dan kata sandi ke database. Mengembalikan true kalau berhasil masuk.
    public async Task<bool> MasukAsync(string username, string kataSandi, bool ingatSaya)
    {
        if (IsBusy)
            return false;

        IsBusy = true;
        Message = "";
        try
        {
            var hasil = await _auth.MasukAsync(username, kataSandi, ingatSaya);
            if (!hasil.Berhasil)
            {
                Message = hasil.Pesan;
                return false;
            }

            if (hasil.TokenSesi is not null)
                _sesiLokal.Simpan(hasil.TokenSesi);
            else
                _sesiLokal.Hapus();

            SesiAplikasi.Mulai(hasil.Akun!, hasil.TokenSesi);
            _navigateToDashboard();
            return true;
        }
        catch (Exception ex)
        {
            Message = $"Tidak bisa terhubung ke database: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void NavigateToRegister()
    {
        _navigateToRegister();
    }

    public void NavigateToForgotPassword()
    {
        _navigateToForgotPassword();
    }
}
