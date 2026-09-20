namespace ITOpsWorkspace.Infrastructure.Data;

public class SettingsEntity
{
    public int Id { get; set; } = 1;
    public string Username { get; set; } = string.Empty;
    public byte[] EncryptedPassword { get; set; } = Array.Empty<byte>();
    public string CurrentUserDisplayName { get; set; } = string.Empty;
    public string AssignmentGroupName { get; set; } = string.Empty;
    public int IdleTimeoutMinutes { get; set; } = 120;
    public string Role { get; set; } = "Technician";
}