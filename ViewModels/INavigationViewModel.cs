namespace SiagaApi.ViewModels;

public interface INavigationViewModel
{
    string ActivePage { get; }

    void NavigateToPage(string page);
    void Logout();
}