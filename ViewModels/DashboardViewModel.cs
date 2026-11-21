using System;
using System.Collections.ObjectModel;
using SiagaApi.ViewModels.Components;

namespace SiagaApi.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<StatCardViewModel> Statistics { get; } = new();

    private string _lastUpdated = "10.42 WIB";

    public string LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value);
    }

    public DashboardViewModel()
    {
        Statistics.Add(new StatCardViewModel
        {
            Label = "HOTSPOT 24 JAM",
            Value = "142",
            Description = "38 lebih banyak dari kemarin"
        });

        Statistics.Add(new StatCardViewModel
        {
            Label = "CONFIDENCE ≥ 80%",
            Value = "57",
            Description = "Indikasi kuat, belum terverifikasi"
        });

        Statistics.Add(new StatCardViewModel
        {
            Label = "INDEKS RISIKO TERTINGGI",
            Value = "78/100",
            Description = "Rumbai, Pekanbaru · Merah"
        });

        Statistics.Add(new StatCardViewModel
        {
            Label = "LAPORAN WARGA HARI INI",
            Value = "5",
            Description = "2 cocok dengan hotspot"
        });
    }

    public void RefreshData()
    {
        LastUpdated = DateTime.Now.ToString("HH.mm") + " WIB";
    }
}