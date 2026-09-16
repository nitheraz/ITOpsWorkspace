using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class IncidentWorkspaceViewModel : ObservableObject
{
    private readonly object _originViewModel;
    private readonly INavigationService _navigationService;

    public Incident Incident { get; }

    public IncidentWorkspaceViewModel(Incident incident, object originViewModel, INavigationService navigationService)
    {
        Incident = incident;
        _originViewModel = originViewModel;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void Back()
    {
        // Returns to the exact screen instance you came from — so its filters,
        // page number, and scroll position are preserved, not reset.
        _navigationService.NavigateTo(_originViewModel);
    }
}