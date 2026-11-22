using System;

namespace SiagaApi.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly Action _navigateToRegister;
    private readonly Action _navigateToDashboard;
    private readonly Action _navigateToForgotPassword;

    public LoginViewModel(
        Action navigateToRegister,
        Action navigateToDashboard,
        Action navigateToForgotPassword)
    {
        _navigateToRegister = navigateToRegister;
        _navigateToDashboard = navigateToDashboard;
        _navigateToForgotPassword = navigateToForgotPassword;
    }

    public void NavigateToRegister()
    {
        _navigateToRegister();
    }

    public void NavigateToDashboard()
    {
        _navigateToDashboard();
    }

    public void NavigateToForgotPassword()
    {
        _navigateToForgotPassword();
    }
}