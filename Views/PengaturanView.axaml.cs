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

    private void SaveSettings_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PengaturanViewModel viewModel)
        {
            viewModel.SaveSettings();
        }
    }
}