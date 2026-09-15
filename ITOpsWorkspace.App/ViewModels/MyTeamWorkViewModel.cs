using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class MyTeamWorkViewModel : ObservableObject
{
    private readonly IIncidentSource _incidentSource;
    private readonly TeamContext _team;

    public ObservableCollection<Incident> TeamIncidents { get; } = new();
    public ObservableCollection<WorkloadItem> Workload { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Incident? _selectedIncident;

    public MyTeamWorkViewModel(IIncidentSource incidentSource, TeamContext team)
    {
        _incidentSource = incidentSource;
        _team = team;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;

        var incidents = await _incidentSource.GetIncidentsAsync(new IncidentQuery
        {
            AssignedToNames = _team.MemberNames,
            IncludeUnassigned = true
        });

        TeamIncidents.Clear();
        foreach (var incident in incidents)
            TeamIncidents.Add(incident);

        var groups = incidents
            .GroupBy(i => string.IsNullOrWhiteSpace(i.AssignedToName) ? "Unassigned" : i.AssignedToName)
            .Select(g => new WorkloadItem { Name = g.Key, Count = g.Count() })
            .OrderByDescending(w => w.Count);

        Workload.Clear();
        foreach (var item in groups)
            Workload.Add(item);

        IsLoading = false;
    }

    [RelayCommand]
    private void ToggleSelect(Incident incident)
    {
        SelectedIncident = SelectedIncident?.ServiceNowSysId == incident.ServiceNowSysId
            ? null
            : incident;
    }
}