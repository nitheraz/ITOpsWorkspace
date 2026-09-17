namespace ITOpsWorkspace.Core.Models;

public class PlaybookRun
{
    public int Id { get; set; }
    public int PlaybookId { get; set; }
    public string PlaybookTitle { get; set; } = string.Empty;
    public string IncidentSysId { get; set; } = string.Empty;
    public string IncidentNumber { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<PlaybookRunStepState> StepStates { get; set; } = new();
}