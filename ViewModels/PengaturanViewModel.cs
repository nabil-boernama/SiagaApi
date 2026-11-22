using System;
using System.Collections.ObjectModel;

namespace SiagaApi.ViewModels;

public class PengaturanViewModel : ViewModelBase, INavigationViewModel
{
    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;

    public string ActivePage => "Pengaturan";

    public ObservableCollection<string> ProvinsiList { get; } = new()
    {
        "Riau",
        "Jambi",
        "Sumatera Selatan",
        "Kalimantan Barat",
        "Kalimantan Tengah",
        "Kalimantan Selatan"
    };

    public ObservableCollection<string> PetaDasarList { get; } = new()
    {
        "Tanpa peta dasar (jalan offline)",
        "OpenStreetMap",
        "Peta satelit"
    };

    public ObservableCollection<string> IntervalList { get; } = new()
    {
        "Setiap 15 menit",
        "Setiap 30 menit",
        "Setiap 1 jam",
        "Setiap 3 jam",
        "Setiap 6 jam"
    };

    private string _selectedProvinsi = "Riau";
    public string SelectedProvinsi
    {
        get => _selectedProvinsi;
        set => SetProperty(ref _selectedProvinsi, value);
    }

    private bool _allowNationalMode = true;
    public bool AllowNationalMode
    {
        get => _allowNationalMode;
        set => SetProperty(ref _allowNationalMode, value);
    }

    private string _selectedMap = "Tanpa peta dasar (jalan offline)";
    public string SelectedMap
    {
        get => _selectedMap;
        set => SetProperty(ref _selectedMap, value);
    }

    // API KEY - EMPTY BY DEFAULT
    private string _firmsApiKey = "";
    public string FirmsApiKey
    {
        get => _firmsApiKey;
        set => SetProperty(ref _firmsApiKey, value);
    }

    private string _weatherApiKey = "";
    public string WeatherApiKey
    {
        get => _weatherApiKey;
        set => SetProperty(ref _weatherApiKey, value);
    }

    // SMTP - EMPTY BY DEFAULT
    private string _smtpServer = "";
    public string SmtpServer
    {
        get => _smtpServer;
        set => SetProperty(ref _smtpServer, value);
    }

    private string _senderEmail = "";
    public string SenderEmail
    {
        get => _senderEmail;
        set => SetProperty(ref _senderEmail, value);
    }

    private string _selectedInterval = "Setiap 30 menit";
    public string SelectedInterval
    {
        get => _selectedInterval;
        set => SetProperty(ref _selectedInterval, value);
    }

    private string _message = "";
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public PengaturanViewModel(Action<string> navigateToPage, Action logout)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;
    }

    public void SaveSettings()
    {
        if (string.IsNullOrWhiteSpace(SmtpServer))
        {
            Message = "Server SMTP wajib diisi.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SenderEmail) ||
            !SenderEmail.Contains("@") ||
            !SenderEmail.Contains("."))
        {
            Message = "Alamat email pengirim tidak valid.";
            return;
        }

        Message = "Pengaturan berhasil disimpan (simulasi lokal).";
    }

    public void NavigateToPage(string page) => _navigateToPage(page);

    public void Logout() => _logout();
}