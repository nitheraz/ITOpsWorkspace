using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class MyWorkViewModel : ObservableObject
{
    private readonly IIncidentSource _incidentSource;
    private readonly CurrentUserContext _currentUser;

    private List<Incident> _allMyIncidents = new();

    public ObservableCollection<Incident> MyIncidents { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Incident? _selectedIncident;
    [ObservableProperty] private string _statusFilter = "Active";

    public List<string> StatusFilters { get; } = new() { "Active", "All", "Resolved", "Closed" };

    public MyWorkViewModel(IIncidentSource incidentSource, CurrentUserContext currentUser)
    {
        _incidentSource = incidentSource;
        _currentUser = currentUser;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;

        // Server-side filter — ServiceNow only returns incidents assigned to this person,
        // so we're never at risk of missing some because of an overall result limit.
        _allMyIncidents = await _incidentSource.GetIncidentsAsync(
            new IncidentQuery { AssignedToName = _currentUser.DisplayName });

        ApplyFilter();

        IsLoading = false;
    }

    [RelayCommand]
    private void SetStatusFilter(string filter)
    {
        StatusFilter = filter;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        // Safe to filter client-side here — _allMyIncidents is already just this
        // person's tickets (a small set), not the whole instance.
        IEnumerable<Incident> filtered = StatusFilter switch
        {
            "Active" => _allMyIncidents.Where(i =>
                i.StateDisplay.Contains("New", StringComparison.OrdinalIgnoreCase) ||
                i.StateDisplay.Contains("In Progress", StringComparison.OrdinalIgnoreCase)),
            "Resolved" => _allMyIncidents.Where(i =>
                i.StateDisplay.Contains("Resolved", StringComparison.OrdinalIgnoreCase)),
            "Closed" => _allMyIncidents.Where(i =>
                i.StateDisplay.Contains("Closed", StringComparison.OrdinalIgnoreCase)),
            _ => _allMyIncidents
        };

        MyIncidents.Clear();
        foreach (var incident in filtered)
            MyIncidents.Add(incident);
    }

    [RelayCommand]
    private void ToggleSelect(Incident incident)
    {
        SelectedIncident = SelectedIncident?.ServiceNowSysId == incident.ServiceNowSysId
            ? null
            : incident;
    }
}