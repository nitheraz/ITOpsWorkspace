namespace ITOpsWorkspace.Core.Models;

public class IncidentActivity
{
    public int Id { get; set; }

    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}