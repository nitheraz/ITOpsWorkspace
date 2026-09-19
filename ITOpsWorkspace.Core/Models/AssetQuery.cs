namespace ITOpsWorkspace.Core.Models;

public class AssetQuery
{
    public string Keywords { get; set; } = string.Empty;
    public int Limit { get; set; } = 200;
}