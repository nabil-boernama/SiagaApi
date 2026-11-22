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

    private void SaveReport_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is LaporanWargaViewModel viewModel)
        {
            viewModel.AddReport();
        }
    }
}