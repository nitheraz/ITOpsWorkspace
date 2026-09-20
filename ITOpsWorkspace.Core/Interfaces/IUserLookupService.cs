using ITOpsWorkspace.Core.Enums;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IUserLookupService
{
    Task<string?> GetDisplayNameAsync(string instanceUrl, string username, string password);
    Task<List<string>> GetAssignmentGroupNamesAsync(string instanceUrl, string username, string password);
    Task<UserRole> DetermineRoleAsync(string instanceUrl, string username, string password, string assignmentGroupName);
}