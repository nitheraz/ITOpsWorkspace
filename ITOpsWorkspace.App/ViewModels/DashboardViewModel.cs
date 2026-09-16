using CommunityToolkit.Mvvm.ComponentModel;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IIncidentSource _incidentSource;
    private readonly CurrentUserContext _currentUser;

    [ObservableProperty] private int _myOpenCount;
    [ObservableProperty] private int _unassignedCount;
    [ObservableProperty] private int _criticalCount;
    [ObservableProperty] private int _highCount;
    [ObservableProperty] private int _moderateCount;
    [ObservableProperty] private int _lowCount;

    public DashboardViewModel(IIncidentSource incidentSource, CurrentUserContext currentUser)
    {
        _incidentSource = incidentSource;
        _currentUser = currentUser;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        MyOpenCount = await _incidentSource.GetIncidentCountAsync(
            new IncidentQuery { AssignedToName = _currentUser.DisplayName });

        UnassignedCount = await _incidentSource.GetIncidentCountAsync(
            new IncidentQuery { AssignedToIsEmpty = true });

        CriticalCount = await _incidentSource.GetIncidentCountAsync(
            new IncidentQuery { PriorityValue = "1" });

        HighCount = await _incidentSource.GetIncidentCountAsync(
            new IncidentQuery { PriorityValue = "2" });

        ModerateCount = await _incidentSource.GetIncidentCountAsync(
            new IncidentQuery { PriorityValue = "3" });

        LowCount = await _incidentSource.GetIncidentCountAsync(
            new IncidentQuery { PriorityValue = "4" });
    }
}