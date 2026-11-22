using System;
using System.Collections.ObjectModel;
using Avalonia.Media;

namespace SiagaApi.ViewModels;

public class LaporanWargaViewModel : ViewModelBase, INavigationViewModel
{
    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;

    public string ActivePage => "Laporan Warga";

    public ObservableCollection<string> WilayahList { get; } = new()
    {
        "Rumbai, Pekanbaru",
        "Pelalawan",
        "Siak",
        "Rokan Hilir",
        "Kampar",
        "Bengkalis"
    };

    public ObservableCollection<string> KeparahanList { get; } = new()
    {
        "Asap saja",
        "Api terlihat",
        "Api besar"
    };

    public ObservableCollection<LaporanItemViewModel> Reports { get; } = new();

    private string _namaPelapor = "";

    public string NamaPelapor
    {
        get => _namaPelapor;
        set => SetProperty(ref _namaPelapor, value);
    }

    private string _selectedWilayah = "Rumbai, Pekanbaru";

    public string SelectedWilayah
    {
        get => _selectedWilayah;
        set => SetProperty(ref _selectedWilayah, value);
    }

    private string _selectedKeparahan = "Api terlihat";

    public string SelectedKeparahan
    {
        get => _selectedKeparahan;
        set => SetProperty(ref _selectedKeparahan, value);
    }

    private string _deskripsi = "";

    public string Deskripsi
    {
        get => _deskripsi;
        set => SetProperty(ref _deskripsi, value);
    }

    private string _message = "";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public LaporanWargaViewModel(
        Action<string> navigateToPage,
        Action logout)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;

        InitializeReports();
    }

    private void InitializeReports()
    {
        Reports.Add(new LaporanItemViewModel(
            "10.58",
            "Rumbai",
            "Api terlihat",
            "Ya · 2 titik < 3 km",
            "Terverifikasi",
            true));

        Reports.Add(new LaporanItemViewModel(
            "10.31",
            "Rumbai",
            "Asap saja",
            "Ya · 1 titik < 5 km",
            "Terverifikasi",
            true));

        Reports.Add(new LaporanItemViewModel(
            "09.47",
            "Pelalawan",
            "Api terlihat",
            "Sedang diperiksa",
            "Menunggu",
            false));

        Reports.Add(new LaporanItemViewModel(
            "08.12",
            "Siak",
            "Asap saja",
            "Tidak ada titik",
            "Menunggu",
            false));

        Reports.Add(new LaporanItemViewModel(
            "Kemarin",
            "Rokan Hilir",
            "Api besar",
            "Ya · 6 titik < 2 km",
            "Ditindaklanjuti",
            true));
    }

    public void AddReport()
    {
        if (string.IsNullOrWhiteSpace(NamaPelapor))
        {
            Message = "Nama pelapor wajib diisi.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Deskripsi))
        {
            Message = "Deskripsi kejadian wajib diisi.";
            return;
        }

        var report = new LaporanItemViewModel(
            DateTime.Now.ToString("HH.mm"),
            SelectedWilayah.Split(',')[0],
            SelectedKeparahan,
            "Belum diperiksa",
            "Menunggu",
            false);

        Reports.Insert(0, report);

        NamaPelapor = "";
        Deskripsi = "";

        Message = "Laporan berhasil disimpan.";
    }

    public void NavigateToPage(string page)
    {
        _navigateToPage(page);
    }

    public void Logout()
    {
        _logout();
    }
}

public class LaporanItemViewModel
{
    public string Waktu { get; }
    public string Wilayah { get; }
    public string Keparahan { get; }
    public string CocokHotspot { get; }
    public string Status { get; }

    public IBrush StatusBackground { get; }
    public IBrush StatusForeground { get; }

    public LaporanItemViewModel(
        string waktu,
        string wilayah,
        string keparahan,
        string cocokHotspot,
        string status,
        bool verified)
    {
        Waktu = waktu;
        Wilayah = wilayah;
        Keparahan = keparahan;
        CocokHotspot = cocokHotspot;
        Status = status;

        StatusBackground = verified
            ? new SolidColorBrush(Color.Parse("#142B20"))
            : new SolidColorBrush(Color.Parse("#302713"));

        StatusForeground = verified
            ? new SolidColorBrush(Color.Parse("#78C99A"))
            : new SolidColorBrush(Color.Parse("#E6B94F"));
    }
}