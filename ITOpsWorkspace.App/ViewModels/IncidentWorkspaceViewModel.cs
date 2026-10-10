using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.App.Views;
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

    [ObservableProperty]
    private Incident _incident;

    public ObservableCollection<ActivityEntry> Activity { get; } = new();
    public ObservableCollection<IncidentStateOption> StateOptions { get; } = new();
    public ObservableCollection<KnowledgeArticle> TroubleshootingArticles { get; } = new();
    public ObservableCollection<Playbook> MatchedPlaybooks { get; } = new();
    public ObservableCollection<PlaybookRun> ActiveRuns { get; } = new();
    public ObservableCollection<AssistantMessage> ChatMessages { get; } = new();

    [ObservableProperty]
    private PlaybookRun? _activeRun;

    [ObservableProperty]
    private IncidentStateOption? _selectedStateOption;

    [ObservableProperty]
    private string _newWorkNote = string.Empty;

    [ObservableProperty]
    private string _chatInput = string.Empty;

    [ObservableProperty]
    private bool _isLoadingActivity;

    [ObservableProperty]
    private bool _isLoadingArticles;

    [ObservableProperty]
    private bool _isLoadingPlaybooks;

    [ObservableProperty]
    private bool _isChatBusy;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isPlaybookBusy;

    [ObservableProperty]
    private string _actionMessage = string.Empty;

    public IncidentWorkspaceViewModel(
        Incident incident,
        object originViewModel,
        INavigationService navigationService,
        IIncidentSource incidentSource,
        IPlaybookService playbookService,
        CurrentUserContext currentUser,
        IAssistantService assistantService)
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
        try
        {
            IsChatBusy = true;

            var greeting = await _assistantService.AskAsync(
                Incident.Title,
                Incident.Description,
                new List<AssistantMessage>(),
                "");

            ChatMessages.Add(greeting);
        }
        catch (Exception ex)
        {
            ActionMessage = $"The assistant could not load: {ex.Message}";
        }
        finally
        {
            IsChatBusy = false;
        }
    }

    private async Task LoadActivityAsync()
    {
        try
        {
            IsLoadingActivity = true;

            var entries = await _incidentSource.GetActivityAsync(
                Incident.ServiceNowSysId);

            Activity.Clear();

            foreach (var entry in entries)
                Activity.Add(entry);
        }
        catch (Exception ex)
        {
            ActionMessage = $"Activity could not be loaded: {ex.Message}";
        }
        finally
        {
            IsLoadingActivity = false;
        }
    }

    private async Task LoadStateOptionsAsync()
    {
        try
        {
            var options = await _incidentSource.GetIncidentStateOptionsAsync();

            StateOptions.Clear();

            foreach (var option in options)
                StateOptions.Add(option);

            SelectedStateOption = StateOptions.FirstOrDefault(
                option => option.Label == Incident.StateDisplay);
        }
        catch (Exception ex)
        {
            ActionMessage = $"Incident statuses could not be loaded: {ex.Message}";
        }
    }

    private async Task LoadTroubleshootingArticlesAsync()
    {
        try
        {
            IsLoadingArticles = true;

            var articles = await _incidentSource.SearchKnowledgeArticlesAsync(
                Incident.Title);

            TroubleshootingArticles.Clear();

            foreach (var article in articles)
                TroubleshootingArticles.Add(article);
        }
        catch (Exception ex)
        {
            ActionMessage = $"Knowledge articles could not be loaded: {ex.Message}";
        }
        finally
        {
            IsLoadingArticles = false;
        }
    }

    private async Task LoadPlaybooksAsync()
    {
        try
        {
            IsLoadingPlaybooks = true;

            // Load every playbook so the technician can choose.
            var playbooks = await _playbookService.SearchPlaybooksAsync(
                string.Empty);

            var activeRuns = await _playbookService.GetActiveRunsAsync(
                Incident.ServiceNowSysId);

            MatchedPlaybooks.Clear();

            foreach (var playbook in playbooks)
                MatchedPlaybooks.Add(playbook);

            ActiveRuns.Clear();

            foreach (var run in activeRuns)
                ActiveRuns.Add(run);

            // Keep the selected run if it is still active.
            if (ActiveRun is not null)
            {
                ActiveRun = ActiveRuns.FirstOrDefault(
                    run => run.Id == ActiveRun.Id);
            }

            // Otherwise select the most recently started active run.
            ActiveRun ??= ActiveRuns.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ActionMessage = $"Playbooks could not be loaded: {ex.Message}";
        }
        finally
        {
            IsLoadingPlaybooks = false;
        }
    }

    [RelayCommand]
    private async Task ViewAsset()
    {
        if (string.IsNullOrWhiteSpace(Incident.AssetSysId))
            return;

        try
        {
            var assetSource = App.Services.GetRequiredService<IAssetSource>();
            var asset = await assetSource.GetAssetBySysIdAsync(Incident.AssetSysId);

            if (asset is not null)
            {
                _navigationService.NavigateTo(
                    new AssetDetailViewModel(asset, this, _navigationService));
            }
        }
        catch (Exception ex)
        {
            ActionMessage = $"The asset could not be opened: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendChatMessage()
    {
        if (string.IsNullOrWhiteSpace(ChatInput) || IsChatBusy)
            return;

        var messageText = ChatInput.Trim();
        ChatInput = string.Empty;

        ChatMessages.Add(new AssistantMessage
        {
            Role = AssistantRole.User,
            Text = messageText,
            Timestamp = DateTime.UtcNow
        });

        try
        {
            IsChatBusy = true;

            var response = await _assistantService.AskAsync(
                Incident.Title,
                Incident.Description,
                ChatMessages.ToList(),
                messageText);

            ChatMessages.Add(response);
        }
        catch (Exception ex)
        {
            ActionMessage = $"The assistant could not respond: {ex.Message}";
        }
        finally
        {
            IsChatBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenSuggestion(AssistantSuggestion suggestion)
    {
        switch (suggestion.Type)
        {
            case AssistantSuggestionType.KnowledgeArticle:
            case AssistantSuggestionType.WebSearch:
                if (!string.IsNullOrWhiteSpace(suggestion.Url))
                {
                    Process.Start(new ProcessStartInfo(suggestion.Url)
                    {
                        UseShellExecute = true
                    });
                }

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
        if (playbook is null || IsPlaybookBusy)
            return;

        try
        {
            IsPlaybookBusy = true;
            ActionMessage = string.Empty;

            // The service returns an existing active run for this
            // playbook and incident, rather than duplicating it.
            var run = await _playbookService.StartRunAsync(
                playbook.Id,
                Incident.ServiceNowSysId,
                Incident.Number);

            var existingIndex = -1;

            for (var i = 0; i < ActiveRuns.Count; i++)
            {
                if (ActiveRuns[i].Id == run.Id)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
                ActiveRuns[existingIndex] = run;
            else
                ActiveRuns.Insert(0, run);

            ActiveRun = run;
            ActionMessage = $"Playbook selected: {run.PlaybookTitle}.";
        }
        catch (Exception ex)
        {
            ActionMessage = $"The playbook could not be started: {ex.Message}";
        }
        finally
        {
            IsPlaybookBusy = false;
        }
    }

    [RelayCommand]
    private void ResumePlaybook(PlaybookRun run)
    {
        if (run is null)
            return;

        ActiveRun = ActiveRuns.FirstOrDefault(
            activeRun => activeRun.Id == run.Id);

        if (ActiveRun is not null)
            ActionMessage = $"Resumed playbook: {ActiveRun.PlaybookTitle}.";
    }

    [RelayCommand]
    private async Task ToggleRunStep(PlaybookRunStepState state)
    {
        if (state is null || IsPlaybookBusy)
            return;

        try
        {
            IsPlaybookBusy = true;
            ActionMessage = string.Empty;

            await _playbookService.SetStepCheckedAsync(
                state.Id,
                state.IsChecked);
        }
        catch (Exception ex)
        {
            ActionMessage = $"The checklist change could not be saved: {ex.Message}";

            // Reload the persisted run so the checkbox reflects
            // the actual saved state if the update failed.
            await LoadPlaybooksAsync();
        }
        finally
        {
            IsPlaybookBusy = false;
        }
    }

    [RelayCommand]
    private async Task CompleteRun()
    {
        if (ActiveRun is null || IsPlaybookBusy)
            return;

        var completedRunId = ActiveRun.Id;

        try
        {
            IsPlaybookBusy = true;
            ActionMessage = string.Empty;

            await _playbookService.CompleteRunAsync(completedRunId);

            ActiveRuns.Remove(
                ActiveRuns.FirstOrDefault(run => run.Id == completedRunId)!);

            ActiveRun = ActiveRuns.FirstOrDefault();

            ActionMessage = "Playbook completed.";
        }
        catch (Exception ex)
        {
            ActionMessage = ex.Message;
            await LoadPlaybooksAsync();
        }
        finally
        {
            IsPlaybookBusy = false;
        }
    }

    [RelayCommand]
    private void OpenArticle(KnowledgeArticle article)
    {
        if (article is null || string.IsNullOrWhiteSpace(article.Url))
            return;

        Process.Start(new ProcessStartInfo(article.Url)
        {
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task AddWorkNote()
    {
        if (string.IsNullOrWhiteSpace(NewWorkNote) || IsSaving)
            return;

        try
        {
            IsSaving = true;
            ActionMessage = string.Empty;

            await _incidentSource.AddWorkNoteAsync(
                Incident.ServiceNowSysId,
                NewWorkNote.Trim());

            NewWorkNote = string.Empty;

            await RefreshAsync();

            ActionMessage = "Work note added.";
        }
        catch (Exception ex)
        {
            ActionMessage = $"The work note could not be added: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task AssignToMe()
    {
        if (IsSaving)
            return;

        try
        {
            IsSaving = true;
            ActionMessage = string.Empty;

            await _incidentSource.AssignToUserAsync(
                Incident.ServiceNowSysId,
                _currentUser.DisplayName);

            await RefreshAsync();

            ActionMessage = "Assigned to you.";
        }
        catch (Exception ex)
        {
            ActionMessage = $"The incident could not be assigned: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task ApplyStatus()
    {
        if (SelectedStateOption is null || IsSaving)
            return;

        try
        {
            IsSaving = true;
            ActionMessage = string.Empty;

            var selectedState = SelectedStateOption;

            if (string.Equals(
                selectedState.Label,
                "Resolved",
                StringComparison.OrdinalIgnoreCase))
            {
                var resolutionCodes =
                    await _incidentSource.GetIncidentResolutionCodesAsync();

                if (resolutionCodes.Count == 0)
                {
                    MessageBox.Show(
                        "ServiceNow returned no available resolution codes. " +
                        "Check the incident close-code choices and your permissions.",
                        "Resolution Codes Unavailable",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var dialog = new ResolutionDialog(resolutionCodes);

                if (Application.Current?.MainWindow is Window owner &&
                    owner.IsVisible)
                {
                    dialog.Owner = owner;
                }

                if (dialog.ShowDialog() != true)
                    return;

                await _incidentSource.ResolveIncidentAsync(
                    Incident.ServiceNowSysId,
                    selectedState.Value,
                    dialog.SelectedResolutionCode,
                    dialog.ResolutionNotes);
            }
            else
            {
                await _incidentSource.UpdateStateAsync(
                    Incident.ServiceNowSysId,
                    selectedState.Value);
            }

            await RefreshAsync();

            ActionMessage = string.Equals(
                selectedState.Label,
                "Resolved",
                StringComparison.OrdinalIgnoreCase)
                    ? "Incident resolved successfully."
                    : "Status updated.";
        }
        catch (Exception ex)
        {
            ActionMessage = string.Empty;

            MessageBox.Show(
                $"The incident status could not be updated.\n\n{ex.Message}",
                "Status Update Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task RefreshAsync()
    {
        var refreshed = await _incidentSource.GetIncidentBySysIdAsync(
            Incident.ServiceNowSysId);

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