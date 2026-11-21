namespace SiagaApi.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentPage;

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public MainViewModel()
    {
        _currentPage = CreateLoginViewModel();
    }

    private LoginViewModel CreateLoginViewModel()
    {
        return new LoginViewModel(
            NavigateToRegister,
            NavigateToDashboard
        );
    }

    public void NavigateToLogin()
    {
        CurrentPage = CreateLoginViewModel();
    }

    public void NavigateToRegister()
    {
        CurrentPage = new RegisterViewModel(NavigateToLogin);
    }

    public void NavigateToDashboard()
    {
        CurrentPage = new DashboardViewModel(
            NavigateToPage,
            NavigateToLogin
        );
    }

    public void NavigateToPage(string page)
    {
        switch (page)
        {
            case "Dashboard":
                NavigateToDashboard();
                break;

            case "Peta & Simulasi":
                CurrentPage = new PetaSimulasiViewModel(
                    NavigateToPage,
                    NavigateToLogin
                );
                break;

            case "Laporan Warga":
                CurrentPage = new LaporanWargaViewModel(
                    NavigateToPage,
                    NavigateToLogin
                );
                break;

            case "Langganan":
                CurrentPage = new LanggananViewModel(
                    NavigateToPage,
                    NavigateToLogin
                );
                break;

            case "Riwayat & Tren":
                CurrentPage = new RiwayatTrenViewModel(
                    NavigateToPage,
                    NavigateToLogin
                );
                break;

            default:
                CurrentPage = new PagePlaceholderViewModel(
                    page,
                    NavigateToPage,
                    NavigateToLogin
                );
                break;
        }
    }
}