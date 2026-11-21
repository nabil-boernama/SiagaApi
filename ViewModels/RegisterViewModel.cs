using System;

namespace SiagaApi.ViewModels;

public class RegisterViewModel : ViewModelBase
{
    private readonly Action _navigateToLogin;

    public RegisterViewModel(Action navigateToLogin)
    {
        _navigateToLogin = navigateToLogin;
    }

    public void NavigateToLogin()
    {
        _navigateToLogin();
    }
}