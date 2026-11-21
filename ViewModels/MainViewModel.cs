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
        _currentPage = new LoginViewModel(NavigateToRegister);
    }

    public void NavigateToLogin()
    {
        CurrentPage = new LoginViewModel(NavigateToRegister);
    }

    public void NavigateToRegister()
    {
        CurrentPage = new RegisterViewModel(NavigateToLogin);
    }
}