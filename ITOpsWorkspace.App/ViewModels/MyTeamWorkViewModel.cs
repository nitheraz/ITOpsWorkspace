using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class MyTeamWorkViewModel : ObservableObject
{
    private readonly IIncidentSource _incidentSource;
    private readonly TeamContext _team;
    private readonly INavigationService _navigationService;
    private readonly CurrentUserContext _currentUser;
    private readonly IPlaybookService _playbookService;
    private readonly IAssistantService _assistantService;

    private List<Incident> _allTeamIncidents = new();
    private List<Incident> _filteredIncidents = new();

    private const int PageSize = 10;

    public ObservableCollection<Incident> TeamIncidents { get; } = new();
    public ObservableCollection<WorkloadItem> Workload { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusFilter = "Active";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private int _totalPages = 1;

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public List<string> StatusFilters { get; } = new() { "Active", "All", "Resolved", "Closed" };

    public string DebugAssignmentGroupName => string.IsNullOrWhiteSpace(_team.AssignmentGroupName)
        ? "(none — no group found for this user)"
        : _team.AssignmentGroupName;

    public MyTeamWorkViewModel(IIncidentSource incidentSource, TeamContext team, INavigationService navigationService,
        CurrentUserContext currentUser, IPlaybookService playbookService, IAssistantService assistantService)
    {
        _incidentSource = incidentSource;
        _team = team;
        _navigationService = navigationService;
        _currentUser = currentUser;
        _playbookService = playbookService;
        _assistantService = assistantService;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;

        _allTeamIncidents = await _incidentSource.GetIncidentsAsync(new IncidentQuery
        {
            AssignmentGroupName = _team.AssignmentGroupName
        });

        var groups = _allTeamIncidents
            .GroupBy(i => string.IsNullOrWhiteSpace(i.AssignedToName) ? "Unassigned" : i.AssignedToName)
            .Select(g => new WorkloadItem { Name = g.Key, Count = g.Count() })
            .OrderByDescending(w => w.Count);

        Workload.Clear();
        foreach (var item in groups)
            Workload.Add(item);

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
        IEnumerable<Incident> filtered = StatusFilter switch
        {
            "Active" => _allTeamIncidents.Where(i =>
                i.StateDisplay.Contains("New", StringComparison.OrdinalIgnoreCase) ||
                i.StateDisplay.Contains("In Progress", StringComparison.OrdinalIgnoreCase)),
            "Resolved" => _allTeamIncidents.Where(i =>
                i.StateDisplay.Contains("Resolved", StringComparison.OrdinalIgnoreCase)),
            "Closed" => _allTeamIncidents.Where(i =>
                i.StateDisplay.Contains("Closed", StringComparison.OrdinalIgnoreCase)),
            _ => _allTeamIncidents
        };

        _filteredIncidents = filtered.ToList();
        CurrentPage = 1;
        UpdateTotalPages();
        UpdatePageItems();
    }

    private void UpdateTotalPages()
    {
        TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredIncidents.Count / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
    }

    private void UpdatePageItems()
    {
        TeamIncidents.Clear();
        foreach (var incident in _filteredIncidents.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            TeamIncidents.Add(incident);
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            UpdatePageItems();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdatePageItems();
        }
    }

    [RelayCommand]
    private void OpenIncident(Incident incident)
    {
        _navigationService.NavigateTo(new IncidentWorkspaceViewModel(
            incident, this, _navigationService, _incidentSource, _playbookService, _currentUser, _assistantService));
    }
}