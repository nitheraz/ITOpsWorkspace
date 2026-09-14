using ITOpsWorkspace.Core.Enums;

namespace ITOpsWorkspace.Core.Models;

public class Incident
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentStatus Status { get; set; } = IncidentStatus.New;
    public IncidentSource Source { get; set; }

    public int RequesterId { get; set; }
    public User Requester { get; set; } = null!;

    public string LocationText { get; set; } = string.Empty;

    public int? AssetId { get; set; }
    public Asset? Asset { get; set; }

    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<IncidentPriorityFlag> PriorityFlags { get; set; } = new();
    public List<IncidentActivity> Activities { get; set; } = new();
}