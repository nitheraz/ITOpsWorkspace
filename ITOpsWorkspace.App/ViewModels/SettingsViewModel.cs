using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly AppSettingsState _appSettingsState;

    public string InstanceUrl { get; }
    public string DisplayName { get; }
    public string Username { get; }
    public string AssignmentGroupName { get; }

    [ObservableProperty] private int _idleTimeoutMinutes;
    [ObservableProperty] private string _saveMessage = string.Empty;

    public SettingsViewModel(IUserSettingsService userSettingsService, AppSettingsState appSettingsState,
        OrgConnectionSettings orgSettings, UserConnectionSettings userSettings)
    {
        _userSettingsService = userSettingsService;
        _appSettingsState = appSettingsState;

        InstanceUrl = orgSettings.InstanceUrl;
        DisplayName = userSettings.CurrentUserDisplayName;
        Username = userSettings.Username;
        AssignmentGroupName = string.IsNullOrWhiteSpace(userSettings.AssignmentGroupName)
            ? "(none configured)"
            : userSettings.AssignmentGroupName;

        IdleTimeoutMinutes = appSettingsState.IdleTimeoutMinutes;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (IdleTimeoutMinutes < 1)
        {
            SaveMessage = "Idle timeout must be at least 1 minute.";
            return;
        }

        var current = await _userSettingsService.LoadAsync();
        if (current is null) return;

        current.IdleTimeoutMinutes = IdleTimeoutMinutes;
        await _userSettingsService.SaveAsync(current);

        // Apply immediately, without needing to restart the app.
        _appSettingsState.IdleTimeoutMinutes = IdleTimeoutMinutes;

        SaveMessage = "Saved.";
    }
}