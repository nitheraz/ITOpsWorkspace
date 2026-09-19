using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Infrastructure.Integrations;

public class ServiceNowAssetSource : IAssetSource
{
    private readonly HttpClient _client;

    public ServiceNowAssetSource(HttpClient client, ServiceNowOptions options)
    {
        _client = client;
        _client.BaseAddress = new Uri(options.InstanceUrl);

        var authBytes = Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<Asset>> GetAssetsAsync(AssetQuery? query = null)
    {
        query ??= new AssetQuery();

        var fields = "sys_id,name,asset_tag,serial_number,manufacturer,model_id,sys_class_name,assigned_to,location,operational_status";
        var url = $"/api/now/table/cmdb_ci?sysparm_limit={query.Limit}&sysparm_display_value=all&sysparm_fields={fields}";

        if (!string.IsNullOrWhiteSpace(query.Keywords))
        {
            var kw = HttpUtility.UrlEncode(query.Keywords);
            url += $"&sysparm_query=nameLIKE{kw}^ORasset_tagLIKE{kw}^ORserial_numberLIKE{kw}";
        }

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("result");

        var assets = new List<Asset>();
        foreach (var item in results.EnumerateArray())
            assets.Add(MapAsset(item));

        return assets;
    }

    public async Task<Asset?> GetAssetBySysIdAsync(string sysId)
    {
        var fields = "sys_id,name,asset_tag,serial_number,manufacturer,model_id,sys_class_name,assigned_to,location,operational_status";
        var url = $"/api/now/table/cmdb_ci/{sysId}?sysparm_display_value=all&sysparm_fields={fields}";

        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("result");

        return MapAsset(result);
    }

    private static Asset MapAsset(JsonElement item) => new Asset
    {
        ServiceNowSysId = GetRawValue(item, "sys_id"),
        Name = GetDisplayValue(item, "name"),
        AssetTag = GetDisplayValue(item, "asset_tag"),
        SerialNumber = GetDisplayValue(item, "serial_number"),
        Manufacturer = GetDisplayValue(item, "manufacturer"),
        Model = GetDisplayValue(item, "model_id"),
        ClassName = GetDisplayValue(item, "sys_class_name"),
        AssignedToName = GetDisplayValue(item, "assigned_to"),
        SiteName = GetDisplayValue(item, "location"),
        OperationalStatus = GetDisplayValue(item, "operational_status")
    };

    private static string GetRawValue(JsonElement item, string field)
    {
        if (!item.TryGetProperty(field, out var prop)) return string.Empty;
        if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("value", out var v))
            return v.GetString() ?? "";
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : "";
    }

    private static string GetDisplayValue(JsonElement item, string field)
    {
        if (!item.TryGetProperty(field, out var prop)) return string.Empty;
        if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("display_value", out var dv))
            return dv.GetString() ?? "";
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : "";
    }
}