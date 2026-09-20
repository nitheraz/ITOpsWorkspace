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
    public System.Collections.ObjectModel.ObservableCollection<string> AvailableGroups { get; } = new();
    [ObservableProperty] private string? _selectedGroup;

    public string DebugGroupCount => $"({AvailableGroups.Count} group(s) found)";

    public LoginViewModel(IUserSettingsService userSettingsService, IUserLookupService userLookupService, string instanceUrl)
    {
        _userSettingsService = userSettingsService;
        _userLookupService = userLookupService;
        _instanceUrl = instanceUrl;
    }

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
            OnPropertyChanged(nameof(DebugGroupCount));
            return false;
        }

        var singleGroup = groups.Count == 1 ? groups[0] : "";
        await FinalizeSaveAsync(singleGroup);
        return true;
    }

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
        var role = await _userLookupService.DetermineRoleAsync(_instanceUrl, Username, _password, assignmentGroupName);

        await _userSettingsService.SaveAsync(new UserConnectionSettings
        {
            Username = Username,
            Password = _password,
            CurrentUserDisplayName = _displayName,
            AssignmentGroupName = assignmentGroupName,
            Role = role
        });
    }
}