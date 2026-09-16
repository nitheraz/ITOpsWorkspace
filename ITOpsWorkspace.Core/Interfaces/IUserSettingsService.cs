using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IUserSettingsService
{
    Task<bool> HasSettingsAsync();
    Task<UserConnectionSettings?> LoadAsync();
    Task SaveAsync(UserConnectionSettings settings);
    Task ClearAsync();
}