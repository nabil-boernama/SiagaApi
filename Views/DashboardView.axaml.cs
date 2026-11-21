using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void RefreshData_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel viewModel)
        {
            viewModel.RefreshData();
        }
    }
}