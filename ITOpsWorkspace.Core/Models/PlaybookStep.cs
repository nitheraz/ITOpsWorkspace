namespace ITOpsWorkspace.Core.Models;

public class PlaybookStep
{
    public int Id { get; set; }
    public int PlaybookId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
}