
namespace ITOpsWorkspace.Core.Models;

public class IncidentResolutionCode
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    public override string ToString() => Label;
}