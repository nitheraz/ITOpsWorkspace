using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IIncidentSource _incidentSource;

    [ObservableProperty] private int _myOpenCount;
    [ObservableProperty] private int _unassignedCount;

    public ObservableCollection<PriorityFlagCount> PriorityFlagCounts { get; } = new();

    public DashboardViewModel(IIncidentSource incidentSource)
    {
        _incidentSource = incidentSource;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var incidents = await _incidentSource.GetIncidentsAsync();

        MyOpenCount = incidents.Count;
        UnassignedCount = incidents.Count(i => string.IsNullOrWhiteSpace(i.AssignedToName));

        // Placeholder until real PriorityFlag data is wired up (Settings-configured flags)
        PriorityFlagCounts.Clear();
        // e.g. PriorityFlagCounts.Add(new PriorityFlagCount { Label = "Leadership Request", Count = 1 });
    }
}