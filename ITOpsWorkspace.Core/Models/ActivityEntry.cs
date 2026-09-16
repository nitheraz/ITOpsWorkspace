namespace ITOpsWorkspace.Core.Models;

public class ActivityEntry
{
    public string Type { get; set; } = string.Empty; // "Comment" or "Work Note"
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}