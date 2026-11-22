using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class RiwayatTrenView : UserControl
{
    public RiwayatTrenView()
    {
        InitializeComponent();
    }

    // Tag tombol berisi jumlah hari rentang grafik (14 atau 30).
    private void Rentang_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is RiwayatTrenViewModel viewModel &&
            sender is Button { Tag: string hari } &&
            int.TryParse(hari, out int jumlah))
        {
            viewModel.AturRentang(jumlah);
        }
    }
}
