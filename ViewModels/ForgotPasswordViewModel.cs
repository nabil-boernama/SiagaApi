using System;

namespace SiagaApi.ViewModels;

public class ForgotPasswordViewModel : ViewModelBase
{
    private readonly Action _navigateToLogin;

    private string _email = string.Empty;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _statusMessage =
        "Tautan pemulihan akan dikirim ke email yang terdaftar.";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ForgotPasswordViewModel(Action navigateToLogin)
    {
        _navigateToLogin = navigateToLogin;
    }

    public void NavigateToLogin()
    {
        _navigateToLogin();
    }

    public void RequestReset()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            StatusMessage = "Silakan masukkan alamat email terlebih dahulu.";
            return;
        }

        if (!Email.Contains('@') || !Email.Contains('.'))
        {
            StatusMessage = "Format alamat email belum valid.";
            return;
        }

        StatusMessage =
            "Email berhasil divalidasi. Pengiriman tautan pemulihan akan aktif setelah API backend tersedia.";
    }
}