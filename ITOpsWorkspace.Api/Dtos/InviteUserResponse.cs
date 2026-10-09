namespace ITOpsWorkspace.Api.Dtos;

public class InviteUserResponse
{
    public Guid UserId { get; set; }
    public string InvitationToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}