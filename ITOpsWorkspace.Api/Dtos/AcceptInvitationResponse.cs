namespace ITOpsWorkspace.Api.Dtos;

public class AcceptInvitationResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}