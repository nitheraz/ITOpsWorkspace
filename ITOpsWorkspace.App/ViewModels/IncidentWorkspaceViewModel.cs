using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Enums;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class IncidentWorkspaceViewModel : ObservableObject
{
    private readonly object _originViewModel;
    private readonly INavigationService _navigationService;
    private readonly IIncidentSource _incidentSource;
    private readonly IPlaybookService _playbookService;
    private readonly IAssistantService _assistantService;
    private readonly CurrentUserContext _currentUser;

    [ObservableProperty] private Incident _incident;

    public ObservableCollection<ActivityEntry> Activity { get; } = new();
    public ObservableCollection<IncidentStateOption> StateOptions { get; } = new();
    public ObservableCollection<KnowledgeArticle> TroubleshootingArticles { get; } = new();
    public ObservableCollection<Playbook> MatchedPlaybooks { get; } = new();
    public ObservableCollection<AssistantMessage> ChatMessages { get; } = new();

    [ObservableProperty] private PlaybookRun? _activeRun;
    [ObservableProperty] private IncidentStateOption? _selectedStateOption;
    [ObservableProperty] private string _newWorkNote = string.Empty;
    [ObservableProperty] private string _chatInput = string.Empty;
    [ObservableProperty] private bool _isLoadingActivity;
    [ObservableProperty] private bool _isLoadingArticles;
    [ObservableProperty] private bool _isChatBusy;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _actionMessage = string.Empty;

    public IncidentWorkspaceViewModel(Incident incident, object originViewModel,
        INavigationService navigationService, IIncidentSource incidentSource,
        IPlaybookService playbookService, CurrentUserContext currentUser, IAssistantService assistantService)
    {
        _incident = incident;
        _originViewModel = originViewModel;
        _navigationService = navigationService;
        _incidentSource = incidentSource;
        _playbookService = playbookService;
        _currentUser = currentUser;
        _assistantService = assistantService;

        _ = LoadActivityAsync();
        _ = LoadStateOptionsAsync();
        _ = LoadTroubleshootingArticlesAsync();
        _ = LoadPlaybooksAsync();
        _ = LoadInitialAssistantMessageAsync();
    }

    private async Task LoadInitialAssistantMessageAsync()
    {
        IsChatBusy = true;
        var greeting = await _assistantService.AskAsync(Incident.Title, Incident.Description, new List<AssistantMessage>(), "");
        IsChatBusy = false;

        ChatMessages.Add(greeting);
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

    private async Task LoadPlaybooksAsync()
    {
        var existingRun = await _playbookService.GetActiveRunAsync(Incident.ServiceNowSysId);
        if (existingRun is not null)
        {
            ActiveRun = existingRun;
            return;
        }

        var matched = await _playbookService.SearchPlaybooksAsync(Incident.Title);
        MatchedPlaybooks.Clear();
        foreach (var p in matched)
            MatchedPlaybooks.Add(p);
    }

    [RelayCommand]
    private async Task ViewAsset()
    {
        if (string.IsNullOrWhiteSpace(Incident.AssetSysId)) return;

        var assetSource = App.Services.GetRequiredService<IAssetSource>();
        var asset = await assetSource.GetAssetBySysIdAsync(Incident.AssetSysId);
        if (asset is not null)
            _navigationService.NavigateTo(new AssetDetailViewModel(asset, this, _navigationService));
    }

    [RelayCommand]
    private async Task SendChatMessage()
    {
        if (string.IsNullOrWhiteSpace(ChatInput)) return;

        var messageText = ChatInput;
        ChatInput = string.Empty;

        ChatMessages.Add(new AssistantMessage
        {
            Role = AssistantRole.User,
            Text = messageText,
            Timestamp = DateTime.UtcNow
        });

        IsChatBusy = true;
        var response = await _assistantService.AskAsync(
            Incident.Title, Incident.Description, ChatMessages.ToList(), messageText);
        IsChatBusy = false;

        ChatMessages.Add(response);
    }

    [RelayCommand]
    private async Task OpenSuggestion(AssistantSuggestion suggestion)
    {
        switch (suggestion.Type)
        {
            case AssistantSuggestionType.KnowledgeArticle:
            case AssistantSuggestionType.WebSearch:
                if (!string.IsNullOrEmpty(suggestion.Url))
                    Process.Start(new ProcessStartInfo(suggestion.Url) { UseShellExecute = true });
                break;

            case AssistantSuggestionType.Playbook:
                if (suggestion.Playbook is not null)
                    await StartPlaybook(suggestion.Playbook);
                break;
        }
    }

    [RelayCommand]
    private async Task StartPlaybook(Playbook playbook)
    {
        var run = await _playbookService.StartRunAsync(playbook.Id, Incident.ServiceNowSysId, Incident.Number);
        ActiveRun = run;
        MatchedPlaybooks.Clear();
    }

    [RelayCommand]
    private async Task ToggleRunStep(PlaybookRunStepState state)
    {
        await _playbookService.SetStepCheckedAsync(state.Id, state.IsChecked);
    }

    [RelayCommand]
    private async Task CompleteRun()
    {
        if (ActiveRun is null) return;

        await _playbookService.CompleteRunAsync(ActiveRun.Id);
        ActiveRun = null;
        ActionMessage = "Playbook completed.";
        await LoadPlaybooksAsync();
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