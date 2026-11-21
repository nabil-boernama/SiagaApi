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
        // Halaman pertama yang ditampilkan adalah Login
        _currentPage = new LoginViewModel();
    }

    public void NavigateToLogin()
    {
        CurrentPage = new LoginViewModel();
    }

    public void NavigateToRegister()
    {
        CurrentPage = new RegisterViewModel();
    }
}