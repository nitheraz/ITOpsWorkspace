namespace ITOpsWorkspace.Core.Models;

public class UserConnectionSettings
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CurrentUserDisplayName { get; set; } = string.Empty;
    public string AssignmentGroupName { get; set; } = string.Empty;
    public int IdleTimeoutMinutes { get; set; } = 120;
}