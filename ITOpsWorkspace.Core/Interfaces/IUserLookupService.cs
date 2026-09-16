namespace ITOpsWorkspace.Core.Interfaces;

public interface IUserLookupService
{
    Task<string?> GetDisplayNameAsync(string instanceUrl, string username, string password);

    // Returns every assignment group the user belongs to (could be zero, one, or several).
    Task<List<string>> GetAssignmentGroupNamesAsync(string instanceUrl, string username, string password);
}