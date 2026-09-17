using ITOpsWorkspace.Core.Enums;

namespace ITOpsWorkspace.Core.Models;

public class AssistantMessage
{
    public AssistantRole Role { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<AssistantSuggestion> Suggestions { get; set; } = new();
}