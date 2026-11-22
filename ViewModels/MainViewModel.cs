using System;
using System.Threading.Tasks;
using SiagaApi.Backend.Data;
using SiagaApi.Backend.Services;

namespace SiagaApi.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly LayananAplikasi _layanan = new();
    private readonly KoneksiDatabase _db;
    private readonly PenyimpanSesiLokal _sesiLokal;
    private readonly AutentikasiService _auth;

    // Memeriksa tiap menit apakah sudah waktunya mengambil data lagi
    // (interval diatur di Pengaturan). Hanya berjalan selama ada operator yang masuk.
    private readonly Avalonia.Threading.DispatcherTimer _pewaktu = new() { Interval = TimeSpan.FromMinutes(1) };
    private bool _sedangSinkronOtomatis;

    private ViewModelBase _currentPage;

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public MainViewModel()
    {
        _db = _layanan.Db;
        _sesiLokal = _layanan.SesiLokal;
        _auth = _layanan.Auth;
        _currentPage = CreateLoginViewModel();

        _pewaktu.Tick += async (_, _) => await SinkronOtomatisAsync();
        _ = CobaMasukOtomatisAsync();
    }

    private LoginViewModel CreateLoginViewModel()
    {
        var login = new LoginViewModel(
            NavigateToRegister,
            NavigateToDashboard,
            NavigateToForgotPassword,
            _auth,
            _sesiLokal
        );
        _ = CekDatabaseAsync(login);
        return login;
    }

    // Halaman Masuk langsung memberi tahu kalau database tidak terjangkau,
    // sebelum operator mengetik apa pun.
    private async Task CekDatabaseAsync(LoginViewModel login)
    {
        bool terhubung = await _db.CekKoneksiAsync();
        login.AturStatusDatabase(terhubung, _db.SumberKonfigurasi);
    }

    // "Ingat saya di komputer ini": kalau ada token tersimpan dan masih
    // berlaku di database, langsung masuk tanpa mengetik kata sandi lagi.
    private async Task CobaMasukOtomatisAsync()
    {
        string? token = _sesiLokal.Baca();
        if (token is null || CurrentPage is not LoginViewModel login)
            return;

        login.IsBusy = true;
        login.Message = "Memeriksa sesi tersimpan...";
        try
        {
            var akun = await _auth.MasukDenganTokenAsync(token);
            if (akun is null)
            {
                // Sesi kedaluwarsa, dicabut, atau akun dinonaktifkan.
                _sesiLokal.Hapus();
                login.Message = "";
                return;
            }

            // Operator sempat pindah halaman (mis. ke Daftar) selagi menunggu: jangan dipaksa masuk.
            if (!ReferenceEquals(CurrentPage, login))
                return;

            SesiAplikasi.Mulai(akun, token);
            NavigateToDashboard();
        }
        catch (Exception)
        {
            // Database tidak terjangkau: token dibiarkan, operator bisa login manual nanti.
            login.Message = "";
        }
        finally
        {
            login.IsBusy = false;
        }
    }

    // Mengambil hotspot dan cuaca baru kalau data terakhir sudah lebih tua dari
    // interval di Pengaturan. Dipanggil saat masuk dan tiap menit oleh pewaktu.
    private async Task SinkronOtomatisAsync()
    {
        if (_sedangSinkronOtomatis || !SesiAplikasi.SudahMasuk)
            return;

        _sedangSinkronOtomatis = true;
        try
        {
            var posko = await _layanan.MuatPoskoAsync();
            var terakhir = await _layanan.Dasbor.AmbilSinkronTerakhirAsync();
            StatusSumber.Perbarui(terakhir, databaseTerhubung: true);

            bool belumPernah = !terakhir.ContainsKey("FIRMS");
            bool sudahWaktunya = belumPernah ||
                                 DateTime.UtcNow - terakhir["FIRMS"].WaktuUtc >= TimeSpan.FromMinutes(posko.IntervalMenit);
            if (!sudahWaktunya)
                return;

            // Pengambilan pertama mengisi 7 hari sekaligus supaya Riwayat & Tren punya data.
            await _layanan.Sinkron.JalankanAsync(riwayat7Hari: belumPernah);
            await _layanan.PerbaruiStatusAsync();

            // Dashboard yang sedang terbuka langsung menampilkan data baru.
            if (CurrentPage is DashboardViewModel dasbor)
                await dasbor.MuatAsync();
        }
        catch (Exception)
        {
            // Database tidak terjangkau: status di sidebar yang memberi tahu operator.
            await _layanan.PerbaruiStatusAsync();
        }
        finally
        {
            _sedangSinkronOtomatis = false;
        }
    }

    private void MulaiPembaruanOtomatis()
    {
        if (_pewaktu.IsEnabled)
            return;

        _pewaktu.Start();
        _ = SinkronOtomatisAsync();
    }

    // Keluar: cabut sesi di database dan di komputer ini, lalu kembali ke login.
    public void Logout()
    {
        _pewaktu.Stop();
        string? token = SesiAplikasi.Token;
        SesiAplikasi.Akhiri();
        _sesiLokal.Hapus();

        if (token is not null)
            _ = CabutSesiAsync(token);

        NavigateToLogin();
    }

    private async Task CabutSesiAsync(string token)
    {
        try
        {
            await _auth.KeluarAsync(token);
        }
        catch (Exception)
        {
            // Gagal mencabut di database tidak menghalangi keluar: token lokal
            // sudah dihapus dan sesi di database kedaluwarsa sendiri.
        }
    }

    public void NavigateToLogin()
    {
        CurrentPage = CreateLoginViewModel();
    }

    public void NavigateToRegister()
    {
        CurrentPage = new RegisterViewModel(NavigateToLogin, _auth);
    }

    public void NavigateToForgotPassword()
    {
        CurrentPage = new ForgotPasswordViewModel(NavigateToLogin);
    }

    public void NavigateToDashboard()
    {
        // Halaman di balik login hanya bisa dibuka kalau sudah masuk.
        if (!SesiAplikasi.SudahMasuk)
        {
            NavigateToLogin();
            return;
        }

        MulaiPembaruanOtomatis();

        CurrentPage = new DashboardViewModel(
            NavigateToPage,
            Logout,
            _layanan,
            BukaSimulasi
        );
    }

    // Dari tombol "Simulasikan klaster ini" di Dashboard: buka halaman simulasi
    // langsung untuk kabupaten yang sedang dipilih.
    public void BukaSimulasi(int wilayahId)
    {
        if (!SesiAplikasi.SudahMasuk)
        {
            NavigateToLogin();
            return;
        }

        CurrentPage = new PetaSimulasiViewModel(NavigateToPage, Logout, _layanan, wilayahId);
    }

    public void NavigateToPage(string page)
    {
        if (!SesiAplikasi.SudahMasuk)
        {
            NavigateToLogin();
            return;
        }

        switch (page)
        {
            case "Dashboard":
                NavigateToDashboard();
                break;

            case "Peta & Simulasi":
                CurrentPage = new PetaSimulasiViewModel(
                    NavigateToPage,
                    Logout,
                    _layanan
                );
                break;

            case "Laporan Warga":
                CurrentPage = new LaporanWargaViewModel(
                    NavigateToPage,
                    Logout,
                    _db
                );
                break;

            case "Langganan":
                CurrentPage = new LanggananViewModel(
                    NavigateToPage,
                    Logout,
                    _layanan
                );
                break;

            case "Riwayat & Tren":
                CurrentPage = new RiwayatTrenViewModel(
                    NavigateToPage,
                    Logout,
                    _layanan
                );
                break;

            case "Pengaturan":
                CurrentPage = new PengaturanViewModel(
                    NavigateToPage,
                    Logout,
                    _layanan
                );
                break;

            default:
                CurrentPage = new PagePlaceholderViewModel(
                    page,
                    NavigateToPage,
                    Logout
                );
                break;
        }
    }
}