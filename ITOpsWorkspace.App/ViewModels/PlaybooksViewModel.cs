using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class PlaybookStepEditItem : ObservableObject
{
    [ObservableProperty] private string _text = string.Empty;
}

public partial class PlaybooksViewModel : ObservableObject
{
    private readonly IPlaybookService _playbookService;

    public ObservableCollection<Playbook> Playbooks { get; } = new();
    public ObservableCollection<PlaybookStepEditItem> EditingSteps { get; } = new();

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private int _editingPlaybookId;
    [ObservableProperty] private string _editingTitle = string.Empty;
    [ObservableProperty] private string _editingDescription = string.Empty;

    public PlaybooksViewModel(IPlaybookService playbookService)
    {
        _playbookService = playbookService;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var all = await _playbookService.GetAllPlaybooksAsync();
        Playbooks.Clear();
        foreach (var p in all)
            Playbooks.Add(p);
    }

    [RelayCommand]
    private void NewPlaybook()
    {
        EditingPlaybookId = 0;
        EditingTitle = string.Empty;
        EditingDescription = string.Empty;
        EditingSteps.Clear();
        EditingSteps.Add(new PlaybookStepEditItem());
        IsEditing = true;
    }

    [RelayCommand]
    private void EditPlaybook(Playbook playbook)
    {
        EditingPlaybookId = playbook.Id;
        EditingTitle = playbook.Title;
        EditingDescription = playbook.Description;
        EditingSteps.Clear();
        foreach (var step in playbook.Steps.OrderBy(s => s.Order))
            EditingSteps.Add(new PlaybookStepEditItem { Text = step.Text });
        IsEditing = true;
    }

    [RelayCommand]
    private void AddStep()
    {
        EditingSteps.Add(new PlaybookStepEditItem());
    }

    [RelayCommand]
    private void RemoveStep(PlaybookStepEditItem step)
    {
        EditingSteps.Remove(step);
    }

    [RelayCommand]
    private async Task SavePlaybook()
    {
        if (string.IsNullOrWhiteSpace(EditingTitle)) return;

        var playbook = new Playbook
        {
            Id = EditingPlaybookId,
            Title = EditingTitle,
            Description = EditingDescription,
            Steps = EditingSteps
                .Select((s, index) => new PlaybookStep { Order = index, Text = s.Text })
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .ToList()
        };

        await _playbookService.SavePlaybookAsync(playbook);
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeletePlaybook(Playbook playbook)
    {
        await _playbookService.DeletePlaybookAsync(playbook.Id);
        await LoadAsync();
    }
}