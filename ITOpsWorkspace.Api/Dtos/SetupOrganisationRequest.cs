namespace ITOpsWorkspace.Api.Dtos;

public class SetupOrganisationRequest
{
    public string OrganisationName { get; set; } = string.Empty;
    public string AdminName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}