namespace ITOpsWorkspace.Core.Models;

public class IncidentQuery
{
    public string? AssignedToName { get; set; }
    public List<string> AssignedToNames { get; set; } = new();
    public bool IncludeUnassigned { get; set; }
    public int Limit { get; set; } = 200;
}