using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class RegisterView : UserControl
{
    public RegisterView()
    {
        InitializeComponent();
    }

    private void Login_Click(object? sender, RoutedEventArgs e)
    {
        (DataContext as RegisterViewModel)?.NavigateToLogin();
    }

    private async void CreateAccount_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not RegisterViewModel viewModel)
            return;

        bool berhasil = await viewModel.DaftarAsync(
            FullNameTextBox.Text ?? "",
            UsernameTextBox.Text ?? "",
            EmailTextBox.Text ?? "",
            PasswordTextBox.Text ?? "",
            ConfirmPasswordTextBox.Text ?? "",
            InvitationCodeTextBox.Text ?? "");

        // Kata sandi tidak dibiarkan tertinggal di layar, berhasil atau tidak.
        PasswordTextBox.Text = "";
        ConfirmPasswordTextBox.Text = "";

        if (berhasil)
        {
            FullNameTextBox.Text = "";
            UsernameTextBox.Text = "";
            EmailTextBox.Text = "";
            InvitationCodeTextBox.Text = "";
        }
    }
}
