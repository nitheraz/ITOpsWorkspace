namespace ITOpsWorkspace.Core.Models;

public class IncidentQuery
{
    public string? AssignedToName { get; set; }
    public string? AssignmentGroupName { get; set; }
    public bool AssignedToIsEmpty { get; set; }
    public string? PriorityValue { get; set; } // raw ServiceNow value: "1"=Critical, "2"=High, "3"=Moderate, "4"=Low
    public int Limit { get; set; } = 200;
}