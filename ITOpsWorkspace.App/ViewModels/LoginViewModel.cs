using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly IUserLookupService _userLookupService;
    private readonly string _instanceUrl;

    private string _password = string.Empty;
    private string _displayName = string.Empty;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoggingIn;

    [ObservableProperty] private bool _isChoosingGroup;
    public ObservableCollection<string> AvailableGroups { get; } = new();
    [ObservableProperty] private string? _selectedGroup;

    public LoginViewModel(IUserSettingsService userSettingsService, IUserLookupService userLookupService, string instanceUrl)
    {
        _userSettingsService = userSettingsService;
        _userLookupService = userLookupService;
        _instanceUrl = instanceUrl;
    }

    // Step 1: verify username/password, then figure out the group situation.
    // Returns true only if login is fully complete (no group choice needed).
    // If a choice is needed, IsChoosingGroup becomes true and the caller should
    // wait for ConfirmGroupSelection instead of closing the window.
    public async Task<bool> LoginAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Username and password are required.";
            return false;
        }

        IsLoggingIn = true;
        ErrorMessage = string.Empty;

        var displayName = await _userLookupService.GetDisplayNameAsync(_instanceUrl, Username, password);
        if (displayName is null)
        {
            IsLoggingIn = false;
            ErrorMessage = "Login failed. Check your username and password.";
            return false;
        }

        var groups = await _userLookupService.GetAssignmentGroupNamesAsync(_instanceUrl, Username, password);

        IsLoggingIn = false;
        _password = password;
        _displayName = displayName;

        if (groups.Count > 1)
        {
            AvailableGroups.Clear();
            foreach (var g in groups)
                AvailableGroups.Add(g);
            SelectedGroup = groups[0];
            IsChoosingGroup = true;
            return false; // not done yet — waiting on group selection
        }

        var singleGroup = groups.Count == 1 ? groups[0] : "";
        await FinalizeSaveAsync(singleGroup);
        return true;
    }

    // Step 2 (only called when IsChoosingGroup is true): finish login with the chosen group.
    public async Task<bool> ConfirmGroupSelectionAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedGroup))
        {
            ErrorMessage = "Please select a group.";
            return false;
        }

        await FinalizeSaveAsync(SelectedGroup);
        return true;
    }

    private async Task FinalizeSaveAsync(string assignmentGroupName)
    {
        await _userSettingsService.SaveAsync(new UserConnectionSettings
        {
            Username = Username,
            Password = _password,
            CurrentUserDisplayName = _displayName,
            AssignmentGroupName = assignmentGroupName
        });
    }
}