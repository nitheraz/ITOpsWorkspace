using CommunityToolkit.Mvvm.ComponentModel;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class OrgSetupViewModel : ObservableObject
{
    private readonly IOrgSettingsService _orgSettingsService;

    [ObservableProperty] private string _instanceUrl = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public OrgSetupViewModel(IOrgSettingsService orgSettingsService)
    {
        _orgSettingsService = orgSettingsService;
    }

    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(InstanceUrl))
        {
            ErrorMessage = "Instance URL is required.";
            return false;
        }

        await _orgSettingsService.SaveAsync(new OrgConnectionSettings
        {
            InstanceUrl = InstanceUrl.TrimEnd('/')
        });

        return true;
    }
}