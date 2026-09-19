using ITOpsWorkspace.Core.Enums;

namespace ITOpsWorkspace.Core.Models;

public class Incident
{
    public int Id { get; set; }
    public string ServiceNowSysId { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; } = IncidentStatus.New;
    public IncidentSource Source { get; set; }

    public ServiceNowPriority Priority { get; set; }
    public string PriorityDisplay { get; set; } = string.Empty;
    public string StateDisplay { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string AssignedToName { get; set; } = string.Empty;

    public string AssetSysId { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;

    public string LocationText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<IncidentActivity> Activities { get; set; } = new();
}