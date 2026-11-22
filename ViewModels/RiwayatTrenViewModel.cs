using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SiagaApi.ViewModels;

public class RiwayatTrenViewModel : ViewModelBase, INavigationViewModel
{
    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;

    public string ActivePage => "Riwayat & Tren";

    public ObservableCollection<string> WilayahList { get; } = new()
    {
        "Seluruh Provinsi Riau",
        "Rumbai",
        "Pelalawan",
        "Siak",
        "Rokan Hilir"
    };

    public ObservableCollection<HotspotHarianViewModel> HotspotHarian { get; } = new();

    private string _selectedWilayah = "Seluruh Provinsi Riau";
    public string SelectedWilayah
    {
        get => _selectedWilayah;
        set
        {
            if (SetProperty(ref _selectedWilayah, value))
            {
                LoadData();
            }
        }
    }

    public string TotalHotspot { get; private set; } = "1.108";
    public string PuncakHarian { get; private set; } = "142";
    public string RataRataHarian { get; private set; } = "79";

    public string TanggalPuncak { get; private set; } = "25 Oktober (hari ini)";
    public string PerbandinganRataRata { get; private set; } = "Naik dari 51 pekan lalu";

    public RiwayatTrenViewModel(Action<string> navigateToPage, Action logout)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;

        LoadData();
    }

    private void LoadData()
    {
        int[] values = SelectedWilayah switch
        {
            "Rumbai" => new[] { 8, 12, 15, 18, 16, 21, 24, 29, 32, 38, 35, 42, 39, 48 },
            "Pelalawan" => new[] { 12, 16, 20, 18, 24, 28, 31, 38, 42, 46, 43, 51, 47, 58 },
            "Siak" => new[] { 5, 8, 11, 10, 13, 16, 19, 21, 24, 27, 25, 30, 28, 34 },
            "Rokan Hilir" => new[] { 4, 6, 8, 9, 11, 12, 15, 18, 20, 22, 21, 25, 23, 29 },
            _ => new[] { 30, 36, 41, 52, 47, 58, 66, 81, 92, 104, 97, 118, 104, 142 }
        };

        HotspotHarian.Clear();

        int max = values.Max();

        for (int i = 0; i < values.Length; i++)
        {
            HotspotHarian.Add(new HotspotHarianViewModel(
                (i + 12).ToString(),
                values[i],
                Math.Max(24, values[i] * 273.0 / max),
                values[i] == max
            ));
        }

        int total = values.Sum();
        int peak = max;
        int average = (int)Math.Round(values.Average());

        TotalHotspot = total.ToString("N0");
        PuncakHarian = peak.ToString();
        RataRataHarian = average.ToString();

        TanggalPuncak = "25 Oktober (hari ini)";
        PerbandinganRataRata = SelectedWilayah == "Seluruh Provinsi Riau"
            ? "Naik dari 51 pekan lalu"
            : "Data simulasi wilayah terpilih";

        OnPropertyChanged(nameof(TotalHotspot));
        OnPropertyChanged(nameof(PuncakHarian));
        OnPropertyChanged(nameof(RataRataHarian));
        OnPropertyChanged(nameof(TanggalPuncak));
        OnPropertyChanged(nameof(PerbandinganRataRata));
    }

    public void NavigateToPage(string page) => _navigateToPage(page);

    public void Logout() => _logout();
}

public class HotspotHarianViewModel
{
    public string Tanggal { get; }
    public string Jumlah { get; }
    public double TinggiBatang { get; }
    public bool IsPeak { get; }

    public string BarColor => IsPeak ? "#F15A24" : "#7A3A20";

    public HotspotHarianViewModel(string tanggal, int jumlah, double tinggiBatang, bool isPeak)
    {
        Tanggal = tanggal;
        Jumlah = jumlah.ToString();
        TinggiBatang = tinggiBatang;
        IsPeak = isPeak;
    }
}