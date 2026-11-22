using Avalonia.Controls;
using Avalonia.Interactivity;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class PetaSimulasiView : UserControl
{
    public PetaSimulasiView()
    {
        InitializeComponent();
    }

    private void RunSimulation_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is PetaSimulasiViewModel viewModel)
        {
            viewModel.RunSimulation();
        }
    }
}