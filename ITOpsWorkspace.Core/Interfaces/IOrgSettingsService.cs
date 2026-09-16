using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IOrgSettingsService
{
    Task<bool> HasSettingsAsync();
    Task<OrgConnectionSettings?> LoadAsync();
    Task SaveAsync(OrgConnectionSettings settings);
}