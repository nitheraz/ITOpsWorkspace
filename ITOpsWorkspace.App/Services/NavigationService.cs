namespace ITOpsWorkspace.App.Services;

public class NavigationService : INavigationService
{
    public event Action<object>? CurrentViewModelChanged;

    public void NavigateTo(object viewModel)
    {
        CurrentViewModelChanged?.Invoke(viewModel);
    }
}