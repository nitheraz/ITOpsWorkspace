namespace ITOpsWorkspace.Api.Dtos;

public class SetupOrganisationResponse
{
    public Guid OrganisationId { get; set; }
    public Guid UserId { get; set; }
    public string InvitationToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}