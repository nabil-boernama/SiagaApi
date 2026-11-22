using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class ForgotPasswordView : UserControl
{
    public ForgotPasswordView()
    {
        InitializeComponent();
    }

    private void SendResetLink_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is ForgotPasswordViewModel viewModel)
        {
            viewModel.RequestReset();
        }
    }

    private void BackToLogin_Click(
        object? sender,
        Avalonia.Input.PointerPressedEventArgs e)
    {
        if (DataContext is ForgotPasswordViewModel viewModel)
        {
            viewModel.NavigateToLogin();
        }
    }
}