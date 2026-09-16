using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class IncidentWorkspaceViewModel : ObservableObject
{
    private readonly object _originViewModel;
    private readonly INavigationService _navigationService;
    private readonly IIncidentSource _incidentSource;
    private readonly CurrentUserContext _currentUser;

    [ObservableProperty] private Incident _incident;

    public ObservableCollection<ActivityEntry> Activity { get; } = new();
    public ObservableCollection<IncidentStateOption> StateOptions { get; } = new();
    public ObservableCollection<KnowledgeArticle> TroubleshootingArticles { get; } = new();

    [ObservableProperty] private IncidentStateOption? _selectedStateOption;
    [ObservableProperty] private string _newWorkNote = string.Empty;
    [ObservableProperty] private bool _isLoadingActivity;
    [ObservableProperty] private bool _isLoadingArticles;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _actionMessage = string.Empty;

    public IncidentWorkspaceViewModel(Incident incident, object originViewModel,
        INavigationService navigationService, IIncidentSource incidentSource, CurrentUserContext currentUser)
    {
        _incident = incident;
        _originViewModel = originViewModel;
        _navigationService = navigationService;
        _incidentSource = incidentSource;
        _currentUser = currentUser;

        _ = LoadActivityAsync();
        _ = LoadStateOptionsAsync();
        _ = LoadTroubleshootingArticlesAsync();
    }

    private async Task LoadActivityAsync()
    {
        IsLoadingActivity = true;
        var entries = await _incidentSource.GetActivityAsync(Incident.ServiceNowSysId);
        Activity.Clear();
        foreach (var entry in entries)
            Activity.Add(entry);
        IsLoadingActivity = false;
    }

    private async Task LoadStateOptionsAsync()
    {
        var options = await _incidentSource.GetIncidentStateOptionsAsync();
        StateOptions.Clear();
        foreach (var option in options)
            StateOptions.Add(option);

        SelectedStateOption = StateOptions.FirstOrDefault(o => o.Label == Incident.StateDisplay);
    }

    private async Task LoadTroubleshootingArticlesAsync()
    {
        IsLoadingArticles = true;
        var articles = await _incidentSource.SearchKnowledgeArticlesAsync(Incident.Title);
        TroubleshootingArticles.Clear();
        foreach (var article in articles)
            TroubleshootingArticles.Add(article);
        IsLoadingArticles = false;
    }

    [RelayCommand]
    private void OpenArticle(KnowledgeArticle article)
    {
        Process.Start(new ProcessStartInfo(article.Url) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task AddWorkNote()
    {
        if (string.IsNullOrWhiteSpace(NewWorkNote)) return;

        IsSaving = true;
        ActionMessage = string.Empty;

        await _incidentSource.AddWorkNoteAsync(Incident.ServiceNowSysId, NewWorkNote);
        NewWorkNote = string.Empty;

        await RefreshAsync();

        IsSaving = false;
        ActionMessage = "Work note added.";
    }

    [RelayCommand]
    private async Task AssignToMe()
    {
        IsSaving = true;
        ActionMessage = string.Empty;

        await _incidentSource.AssignToUserAsync(Incident.ServiceNowSysId, _currentUser.DisplayName);

        await RefreshAsync();

        IsSaving = false;
        ActionMessage = "Assigned to you.";
    }

    [RelayCommand]
    private async Task ApplyStatus()
    {
        if (SelectedStateOption is null) return;

        IsSaving = true;
        ActionMessage = string.Empty;

        await _incidentSource.UpdateStateAsync(Incident.ServiceNowSysId, SelectedStateOption.Value);

        await RefreshAsync();

        IsSaving = false;
        ActionMessage = "Status updated.";
    }

    private async Task RefreshAsync()
    {
        var refreshed = await _incidentSource.GetIncidentBySysIdAsync(Incident.ServiceNowSysId);
        if (refreshed is not null)
            Incident = refreshed;

        await LoadActivityAsync();
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateTo(_originViewModel);
    }
}