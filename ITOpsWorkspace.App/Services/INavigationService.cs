namespace ITOpsWorkspace.App.Services;

public interface INavigationService
{
    event Action<object>? CurrentViewModelChanged;
    void NavigateTo(object viewModel);
}