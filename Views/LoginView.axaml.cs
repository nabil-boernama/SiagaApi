using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is LoginViewModel viewModel)
            {
                RegisterLink.PointerPressed += (_, _) =>
                {
                    viewModel.NavigateToRegister();
                };
            }
        };
    }

    private void PreviewDashboard_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.NavigateToDashboard();
        }
    }

    private void LoginButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        // Autentikasi akan diimplementasikan nanti.
    }
}