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

    private void AddSubscription_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LanggananViewModel viewModel)
        {
            viewModel.AddSubscription();
        }
    }
}