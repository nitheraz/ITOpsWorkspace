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

    public int? RequesterId { get; set; }
    public User? Requester { get; set; }

    public string LocationText { get; set; } = string.Empty;

    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }

    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<IncidentActivity> Activities { get; set; } = new();
}