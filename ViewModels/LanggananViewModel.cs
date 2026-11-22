using System.Linq;
using System.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Avalonia.Media;

namespace SiagaApi.ViewModels;

public class LanggananViewModel : ViewModelBase, INavigationViewModel
{
    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;

    public string ActivePage => "Langganan";

    public ObservableCollection<string> WilayahList { get; } = new()
    {
        "Rumbai, Pekanbaru",
        "Pelalawan",
        "Siak",
        "Rokan Hilir",
        "Kampar",
        "Bengkalis"
    };

    public ObservableCollection<LanggananItemViewModel> Subscriptions { get; } = new();
    public ObservableCollection<NotifikasiItemViewModel> Notifications { get; } = new();

    private string _email = "";
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _selectedWilayah = "Rumbai, Pekanbaru";
    public string SelectedWilayah
    {
        get => _selectedWilayah;
        set => SetProperty(ref _selectedWilayah, value);
    }

    private string _message = "";
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string ActiveCount => $"{Subscriptions.Count(x => x.Status == "Aktif")} AKTIF";

    public LanggananViewModel(Action<string> navigateToPage, Action logout)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;

        InitializeSubscriptions();
        InitializeNotifications();
    }

    private void InitializeSubscriptions()
    {
        Subscriptions.Add(new LanggananItemViewModel("budi@example.com", "Rumbai", "12 Okt", "Aktif"));
        Subscriptions.Add(new LanggananItemViewModel("sari@example.com", "Pelalawan", "14 Okt", "Aktif"));
        Subscriptions.Add(new LanggananItemViewModel("sari@example.com", "Siak", "14 Okt", "Aktif"));
        Subscriptions.Add(new LanggananItemViewModel("andi@example.com", "Rokan Hilir", "20 Okt", "Dijeda"));
    }

    private void InitializeNotifications()
    {
        Notifications.Add(new NotifikasiItemViewModel("10.44", "budi@example.com", "Rumbai · confidence 92%", "Terkirim"));
        Notifications.Add(new NotifikasiItemViewModel("09.18", "sari@example.com", "Pelalawan · confidence 76%", "Terkirim"));
        Notifications.Add(new NotifikasiItemViewModel("09.18", "sari@example.com", "Pelalawan · hotspot sama", "Dilewati"));
    }

    public void AddSubscription()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            Message = "Email warga wajib diisi.";
            return;
        }

        if (!Email.Contains("@") || !Email.Contains("."))
        {
            Message = "Format email belum valid.";
            return;
        }

        bool alreadyExists = Subscriptions.Any(x =>
            x.Email.Equals(Email.Trim(), StringComparison.OrdinalIgnoreCase) &&
            x.Wilayah == SelectedWilayah.Split(',')[0]);

        if (alreadyExists)
        {
            Message = "Email tersebut sudah berlangganan di wilayah ini.";
            return;
        }

        Subscriptions.Insert(0, new LanggananItemViewModel(
            Email.Trim(),
            SelectedWilayah.Split(',')[0],
            DateTime.Now.ToString("dd MMM"),
            "Aktif"
        ));

        Email = "";
        Message = "Langganan berhasil ditambahkan.";
        OnPropertyChanged(nameof(ActiveCount));
    }

    public void NavigateToPage(string page) => _navigateToPage(page);

    public void Logout() => _logout();
}

public class LanggananItemViewModel
{
    public string Email { get; }
    public string Wilayah { get; }
    public string Sejak { get; }
    public string Status { get; }

    public IBrush StatusBackground { get; }
    public IBrush StatusForeground { get; }

    public LanggananItemViewModel(string email, string wilayah, string sejak, string status)
    {
        Email = email;
        Wilayah = wilayah;
        Sejak = sejak;
        Status = status;

        bool active = status == "Aktif";
        StatusBackground = new SolidColorBrush(Color.Parse(active ? "#142B20" : "#20272D"));
        StatusForeground = new SolidColorBrush(Color.Parse(active ? "#78C99A" : "#95A2AD"));
    }
}

public class NotifikasiItemViewModel
{
    public string Waktu { get; }
    public string Email { get; }
    public string Pemicu { get; }
    public string Hasil { get; }

    public IBrush HasilBackground { get; }
    public IBrush HasilForeground { get; }

    public NotifikasiItemViewModel(string waktu, string email, string pemicu, string hasil)
    {
        Waktu = waktu;
        Email = email;
        Pemicu = pemicu;
        Hasil = hasil;

        bool sent = hasil == "Terkirim";
        HasilBackground = new SolidColorBrush(Color.Parse(sent ? "#142B20" : "#20272D"));
        HasilForeground = new SolidColorBrush(Color.Parse(sent ? "#78C99A" : "#95A2AD"));
    }
}