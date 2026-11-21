namespace SiagaApi.ViewModels;

public class PagePlaceholderViewModel : ViewModelBase, INavigationViewModel
{
    private readonly System.Action<string> _navigateToPage;
    private readonly System.Action _logout;

    public string PageTitle { get; }

    public string ActivePage => PageTitle;

    public PagePlaceholderViewModel(
        string pageTitle,
        System.Action<string> navigateToPage,
        System.Action logout)
    {
        PageTitle = pageTitle;
        _navigateToPage = navigateToPage;
        _logout = logout;
    }

    public void NavigateToPage(string page)
    {
        _navigateToPage(page);
    }

    public void Logout()
    {
        _logout();
    }
}