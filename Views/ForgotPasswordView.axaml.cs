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

    private void KembaliKeLogin_Click(
        object? sender,
        RoutedEventArgs e)
    {
        (DataContext as ForgotPasswordViewModel)?.NavigateToLogin();
    }
}
