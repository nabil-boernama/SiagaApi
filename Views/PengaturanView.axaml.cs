using Avalonia.Input.Platform;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class PengaturanView : UserControl
{
    public PengaturanView()
    {
        InitializeComponent();
    }

    // Tag tombol berisi nama tab (lihat PengaturanViewModel.Tab*Nama).
    private void Tab_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PengaturanViewModel viewModel && sender is Button { Tag: string tab })
            viewModel.AturTab(tab);
    }

    // Menyalin kode undangan ke clipboard; ikon berubah jadi centang sebagai tanda berhasil.
    private async void SalinKode_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PengaturanViewModel viewModel ||
            TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
            return;

        await clipboard.SetTextAsync(viewModel.InvitationCode);
        IkonSalin.Nama = "check";
    }

    // Kegagalan ditangani di dalam ViewModel dan tampil lewat Message.
    private async void SaveSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PengaturanViewModel viewModel)
        {
            await viewModel.SaveSettingsAsync();
        }
    }

    private async void GantiKataSandi_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PengaturanViewModel viewModel)
            return;

        await viewModel.GantiKataSandiAsync(
            SandiLamaTextBox.Text ?? "",
            SandiBaruTextBox.Text ?? "",
            SandiKonfirmasiTextBox.Text ?? "");

        // Kata sandi tidak dibiarkan tertinggal di layar, berhasil atau tidak.
        SandiLamaTextBox.Text = "";
        SandiBaruTextBox.Text = "";
        SandiKonfirmasiTextBox.Text = "";
    }

    // Kegagalan database ditangani di dalam ViewModel dan tampil lewat
    // AdminMessage, jadi tidak ada exception yang lolos dari handler ini.
    private async void BuatKodeUndangan_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PengaturanViewModel viewModel)
        {
            await viewModel.BuatKodeUndanganAsync();
        }
    }

    // Baris akun yang tombolnya ditekan, beserta ViewModel halaman ini.
    private bool AmbilAkun(object? sender, out PengaturanViewModel viewModel, out AkunItemViewModel item)
    {
        if (DataContext is PengaturanViewModel vm && sender is Button { DataContext: AkunItemViewModel akun })
        {
            viewModel = vm;
            item = akun;
            return true;
        }

        viewModel = null!;
        item = null!;
        return false;
    }

    private async void SetujuiAkun_Click(object? sender, RoutedEventArgs e)
    {
        if (AmbilAkun(sender, out var viewModel, out var item))
            await viewModel.SetujuiAsync(item);
    }

    private async void TolakAkun_Click(object? sender, RoutedEventArgs e)
    {
        if (AmbilAkun(sender, out var viewModel, out var item))
            await viewModel.TolakAsync(item);
    }

    private async void AktifkanAkun_Click(object? sender, RoutedEventArgs e)
    {
        if (AmbilAkun(sender, out var viewModel, out var item))
            await viewModel.AktifkanAsync(item);
    }

    private async void NonaktifkanAkun_Click(object? sender, RoutedEventArgs e)
    {
        if (AmbilAkun(sender, out var viewModel, out var item))
            await viewModel.NonaktifkanAsync(item);
    }

    private async void JadikanAdmin_Click(object? sender, RoutedEventArgs e)
    {
        if (AmbilAkun(sender, out var viewModel, out var item))
            await viewModel.JadikanAdminAsync(item);
    }

    private async void JadikanOperator_Click(object? sender, RoutedEventArgs e)
    {
        if (AmbilAkun(sender, out var viewModel, out var item))
            await viewModel.JadikanOperatorAsync(item);
    }

    private async void ResetSandi_Click(object? sender, RoutedEventArgs e)
    {
        if (AmbilAkun(sender, out var viewModel, out var item))
            await viewModel.ResetKataSandiAsync(item);
    }
}