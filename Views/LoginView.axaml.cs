using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel viewModel)
            return;

        // Kegagalan (sandi salah, database mati) ditangani di dalam MasukAsync
        // dan tampil lewat Message, jadi tidak ada exception yang lolos ke sini.
        bool berhasil = await viewModel.MasukAsync(
            UsernameTextBox.Text ?? "",
            PasswordTextBox.Text ?? "",
            RememberMeCheckBox.IsChecked == true);

        if (!berhasil)
            PasswordTextBox.Text = "";
    }

    private void LihatSandi_Click(object? sender, RoutedEventArgs e)
    {
        PasswordTextBox.RevealPassword = !PasswordTextBox.RevealPassword;
        IkonLihatSandi.Nama = PasswordTextBox.RevealPassword ? "eye-off" : "eye";
    }

    private void Register_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as LoginViewModel)?.NavigateToRegister();
    }

    private void ForgotPassword_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as LoginViewModel)?.NavigateToForgotPassword();
    }
}
