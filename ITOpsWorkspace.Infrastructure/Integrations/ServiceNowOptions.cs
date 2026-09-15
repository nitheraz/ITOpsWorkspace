namespace ITOpsWorkspace.Infrastructure.Integrations;

public class ServiceNowOptions
{
    public string InstanceUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}