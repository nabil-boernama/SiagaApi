using System;
using SiagaApi.ViewModels;

namespace SiagaApi.ViewModels;

public class PetaSimulasiViewModel : ViewModelBase, INavigationViewModel
{
    private readonly Action<string> _navigateToPage;
    private readonly Action _logout;

    private double _simulationHour = 3;

    public string ActivePage => "Peta & Simulasi";

    public string SimulationTimeText =>
        $"WAKTU SIMULASI · T + {SimulationHour:0} JAM";

    public string SimulationHourStart => "t+0";
    public string SimulationHourEnd => "t+6";

    public string SimulationArea =>
        SimulationHour switch
        {
            < 1 => "≈ 75 ha",
            < 2 => "≈ 145 ha",
            < 3 => "≈ 226 ha",
            < 4 => "≈ 310 ha",
            < 5 => "≈ 405 ha",
            _ => "≈ 520 ha"
        };

    public string SimulationResultLabel =>
        $"HASIL SKENARIO T+{SimulationHour:0}";

    public string SimulationDescription =>
        SimulationHour switch
        {
            < 1 => "Penyebaran masih terkonsentrasi di sekitar klaster hotspot awal.",
            < 2 => "Area berisiko mulai meluas mengikuti arah angin ke timur laut.",
            < 3 => "Jika klaster ini adalah kebakaran aktif, area berisiko meluas ke timur laut dan Desa Palas masuk jangkauan t+6.",
            < 4 => "Zona prediksi meluas dan membutuhkan pemantauan pada wilayah sekitar.",
            < 5 => "Area berisiko semakin luas. Prioritaskan kesiapsiagaan di sekitar batas jangkauan.",
            _ => "Jangkauan simulasi mencapai area terluar. Perlu evaluasi dan pemantauan lanjutan."
        };

    public double SimulationHour
    {
        get => _simulationHour;
        set
        {
            if (SetProperty(ref _simulationHour, value))
            {
                OnPropertyChanged(nameof(SimulationTimeText));
                OnPropertyChanged(nameof(SimulationArea));
                OnPropertyChanged(nameof(SimulationResultLabel));
                OnPropertyChanged(nameof(SimulationDescription));
            }
        }
    }

    public PetaSimulasiViewModel(
        Action<string> navigateToPage,
        Action logout)
    {
        _navigateToPage = navigateToPage;
        _logout = logout;
    }

    public void NavigateToPage(string page)
    {
        _navigateToPage(page);
    }

    public void Logout()
    {
        _logout();
    }

    public void RunSimulation()
    {
        SimulationHour = 3;
    }
}