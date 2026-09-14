namespace ITOpsWorkspace.Core.Models;

public class PriorityFlag
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SortWeight { get; set; }
    public bool IsActive { get; set; } = true;
}