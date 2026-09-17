namespace ITOpsWorkspace.Core.Models;

public class PlaybookRunStepState
{
    public int Id { get; set; }
    public int PlaybookRunId { get; set; }
    public int PlaybookStepId { get; set; }
    public string StepText { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsChecked { get; set; }
    public DateTime? CheckedAt { get; set; }
}