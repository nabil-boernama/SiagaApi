using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class LanggananView : UserControl
{
    public LanggananView()
    {
        InitializeComponent();
    }

    // Tag tombol berisi nama saringan (Semua / Aktif / Dijeda).
    private void Saring_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LanggananViewModel viewModel && sender is Button { Tag: string saringan })
            viewModel.AturSaringan(saringan);
    }

    // Kegagalan database ditangani di dalam ViewModel dan tampil lewat Message.
    private async void AddSubscription_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LanggananViewModel viewModel)
        {
            await viewModel.AddSubscriptionAsync();
        }
    }

    private async void JedaLangganan_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LanggananViewModel viewModel &&
            sender is Button { DataContext: LanggananItemViewModel item })
        {
            await viewModel.JedaAtauLanjutkanAsync(item);
        }
    }

    private async void HapusLangganan_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LanggananViewModel viewModel &&
            sender is Button { DataContext: LanggananItemViewModel item })
        {
            await viewModel.HapusAsync(item);
        }
    }
}