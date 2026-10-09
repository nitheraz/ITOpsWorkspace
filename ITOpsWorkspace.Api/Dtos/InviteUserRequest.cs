namespace ITOpsWorkspace.Api.Dtos;

public class InviteUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Administrator", "ITManager", "Technician", "ReadOnly"
}