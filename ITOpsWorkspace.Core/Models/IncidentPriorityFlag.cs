namespace ITOpsWorkspace.Core.Models;

public class IncidentPriorityFlag
{
    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;

    public int PriorityFlagId { get; set; }
    public PriorityFlag PriorityFlag { get; set; } = null!;
}