namespace ITOpsWorkspace.Core.Models;

public class Playbook
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PlaybookStep> Steps { get; set; } = new();
}