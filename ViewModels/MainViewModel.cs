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
        if (page == "Dashboard")
        {
            NavigateToDashboard();
            return;
        }

        CurrentPage = new PagePlaceholderViewModel(
            page,
            NavigateToPage,
            NavigateToLogin
        );
    }
}