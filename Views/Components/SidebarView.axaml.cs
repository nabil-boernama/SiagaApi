using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using SiagaApi.ViewModels;

namespace SiagaApi.Views.Components;

public partial class SidebarView : UserControl
{
    public SidebarView()
    {
        InitializeComponent();
    }

    private void NavigationButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is string page &&
            DataContext is INavigationViewModel navigation)
        {
            navigation.NavigateToPage(page);
        }
    }

    private void LogoutButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is INavigationViewModel navigation)
        {
            navigation.Logout();
        }
    }

    private void LogoutButton_PointerEntered(
    object? sender,
    PointerEventArgs e)
    {
        LogoutText.TextDecorations =
            Avalonia.Media.TextDecorations.Underline;
    }

    private void LogoutButton_PointerExited(
        object? sender,
        PointerEventArgs e)
    {
        LogoutText.TextDecorations = null;
    }

    private void SidebarView_Loaded(
    object? sender,
    RoutedEventArgs e)
    {
        if (DataContext is not INavigationViewModel navigation)
            return;

        if (Content is not Border border ||
            border.Child is not Grid grid ||
            grid.Children.Count < 2 ||
            grid.Children[1] is not StackPanel menu)
            return;

        foreach (var child in menu.Children)
        {
            if (child is Button button)
            {
                button.Classes.Remove("selected");

                if (button.Tag is string page &&
                    page == navigation.ActivePage)
                {
                    button.Classes.Add("selected");
                }
            }
        }
    }
}