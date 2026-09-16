using CommunityToolkit.Mvvm.ComponentModel;
using ITOpsWorkspace.Core.Interfaces;

namespace ITOpsWorkspace.App.ViewModels;

public partial class LockScreenViewModel : ObservableObject
{
    private readonly IUserLookupService _userLookupService;
    private readonly string _instanceUrl;

    [ObservableProperty] private string _username;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isVerifying;

    public LockScreenViewModel(IUserLookupService userLookupService, string instanceUrl, string username)
    {
        _userLookupService = userLookupService;
        _instanceUrl = instanceUrl;
        _username = username;
    }

    public async Task<bool> UnlockAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Password is required.";
            return false;
        }

        IsVerifying = true;
        ErrorMessage = string.Empty;

        var displayName = await _userLookupService.GetDisplayNameAsync(_instanceUrl, Username, password);

        IsVerifying = false;

        if (displayName is null)
        {
            ErrorMessage = "Incorrect password.";
            return false;
        }

        return true;
    }
}