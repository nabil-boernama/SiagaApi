using System;

namespace SiagaApi.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly Action _navigateToRegister;

    public LoginViewModel(Action navigateToRegister)
    {
        _navigateToRegister = navigateToRegister;
    }

    public void NavigateToRegister()
    {
        _navigateToRegister();
    }
}