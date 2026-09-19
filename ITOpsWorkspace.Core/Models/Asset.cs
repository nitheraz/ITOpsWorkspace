namespace ITOpsWorkspace.Core.Models;

public class Asset
{
    public string ServiceNowSysId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string AssignedToName { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string OperationalStatus { get; set; } = string.Empty;
}