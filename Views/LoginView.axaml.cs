using Avalonia.Controls;
using Avalonia.Input;
using SiagaApi.ViewModels;

namespace SiagaApi.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is LoginViewModel viewModel)
            {
                RegisterLink.PointerPressed += (_, _) =>
                {
                    viewModel.NavigateToRegister();
                };
            }
        };
    }
}