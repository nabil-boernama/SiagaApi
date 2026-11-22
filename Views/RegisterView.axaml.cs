using Avalonia.Controls;
using Avalonia.Input;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class RegisterView : UserControl
{
    public RegisterView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is RegisterViewModel viewModel)
            {
                LoginLink.PointerPressed += (_, _) =>
                {
                    viewModel.NavigateToLogin();
                };
            }
        };
    }
}