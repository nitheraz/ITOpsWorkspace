namespace ITOpsWorkspace.Core.Models;

public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public List<Incident> Incidents { get; set; } = new();
}