namespace ITOpsWorkspace.Core.Models;

public class IncidentQuery
{
    public string? AssignedToName { get; set; }
    public string? AssignmentGroupName { get; set; }
    public int Limit { get; set; } = 200;
}