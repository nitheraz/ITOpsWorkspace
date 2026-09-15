using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ITOpsWorkspace.Core.Enums;
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

    public ObservableCollection<PriorityFlagCount> PriorityFlagCounts { get; } = new();

    public DashboardViewModel(IIncidentSource incidentSource, CurrentUserContext currentUser)
    {
        _incidentSource = incidentSource;
        _currentUser = currentUser;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var incidents = await _incidentSource.GetIncidentsAsync();

        MyOpenCount = incidents.Count(i =>
            !string.IsNullOrWhiteSpace(_currentUser.DisplayName) &&
            i.AssignedToName.Equals(_currentUser.DisplayName, StringComparison.OrdinalIgnoreCase));

        UnassignedCount = incidents.Count(i => string.IsNullOrWhiteSpace(i.AssignedToName));
        CriticalCount = incidents.Count(i => i.Priority == ServiceNowPriority.Critical);
        HighCount = incidents.Count(i => i.Priority == ServiceNowPriority.High);
        ModerateCount = incidents.Count(i => i.Priority == ServiceNowPriority.Moderate);
        LowCount = incidents.Count(i => i.Priority == ServiceNowPriority.Low);

        PriorityFlagCounts.Clear();
    }
}