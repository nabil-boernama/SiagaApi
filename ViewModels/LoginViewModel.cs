using System;

namespace SiagaApi.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly Action _navigateToRegister;
    private readonly Action _navigateToDashboard;

    public LoginViewModel(
        Action navigateToRegister,
        Action navigateToDashboard)
    {
        _navigateToRegister = navigateToRegister;
        _navigateToDashboard = navigateToDashboard;
    }

    public void NavigateToRegister()
    {
        _navigateToRegister();
    }

    public void NavigateToDashboard()
    {
        _navigateToDashboard();
    }
}