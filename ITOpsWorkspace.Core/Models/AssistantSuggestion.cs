using ITOpsWorkspace.Core.Enums;

namespace ITOpsWorkspace.Core.Models;

public class AssistantSuggestion
{
    public AssistantSuggestionType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public Playbook? Playbook { get; set; }
}