using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class LaporanWargaView : UserControl
{
    public LaporanWargaView()
    {
        InitializeComponent();
    }

    private LaporanWargaViewModel? ViewModel => DataContext as LaporanWargaViewModel;

    private void BukaPanel_Click(object? sender, RoutedEventArgs e) => ViewModel?.BukaPanel();

    private void TutupPanel_Click(object? sender, RoutedEventArgs e) => ViewModel?.TutupPanel();

    // Tag tombol berisi nama saringan (Semua / Menunggu / Terverifikasi).
    private void Saring_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string saringan })
            ViewModel?.AturSaringan(saringan);
    }

    // Tag tombol berisi label keparahan (Asap saja / Api terlihat / Api besar).
    private void Keparahan_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string keparahan } && ViewModel is { } viewModel)
            viewModel.SelectedKeparahan = keparahan;
    }

    private async void SaveReport_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            // Kegagalan database sudah ditangkap di dalam AddReportAsync dan
            // ditampilkan lewat Message, jadi tidak ada exception yang lolos ke sini.
            await viewModel.AddReportAsync();
        }
    }
}
