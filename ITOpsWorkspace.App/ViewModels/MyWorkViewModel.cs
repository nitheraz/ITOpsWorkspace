using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class MyWorkViewModel : ObservableObject
{
    private readonly IIncidentSource _incidentSource;
    private readonly CurrentUserContext _currentUser;
    private readonly INavigationService _navigationService;

    private List<Incident> _allMyIncidents = new();
    private List<Incident> _filteredIncidents = new();

    private const int PageSize = 10;

    public ObservableCollection<Incident> MyIncidents { get; } = new();

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

    public MyWorkViewModel(IIncidentSource incidentSource, CurrentUserContext currentUser, INavigationService navigationService)
    {
        _incidentSource = incidentSource;
        _currentUser = currentUser;
        _navigationService = navigationService;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;

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
        MyIncidents.Clear();
        foreach (var incident in _filteredIncidents.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            MyIncidents.Add(incident);
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
        _navigationService.NavigateTo(new IncidentWorkspaceViewModel(incident, this, _navigationService));
    }
}