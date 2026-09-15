using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Infrastructure.Integrations;

public class ServiceNowIncidentSource : IIncidentSource
{
    private readonly HttpClient _client;

    public ServiceNowIncidentSource(HttpClient client, ServiceNowOptions options)
    {
        _client = client;
        _client.BaseAddress = new Uri(options.InstanceUrl);

        var authBytes = Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<Incident>> GetIncidentsAsync()
    {
        var url = "/api/now/table/incident" +
                   "?sysparm_limit=50" +
                   "&sysparm_display_value=all" +
                   "&sysparm_fields=sys_id,number,short_description,description,priority,state,assigned_to,caller_id,sys_created_on";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("result");

        var incidents = new List<Incident>();
        foreach (var item in results.EnumerateArray())
        {
            incidents.Add(new Incident
            {
                ServiceNowSysId = GetString(item, "sys_id"),
                Number = GetString(item, "number"),
                Title = GetString(item, "short_description"),
                Description = GetString(item, "description"),
                PriorityDisplay = GetDisplayValue(item, "priority"),
                StateDisplay = GetDisplayValue(item, "state"),
                RequesterName = GetDisplayValue(item, "caller_id"),
                AssignedToName = GetDisplayValue(item, "assigned_to"),
                CreatedAt = DateTime.TryParse(GetString(item, "sys_created_on"), out var dt) ? dt : DateTime.MinValue
            });
        }
        return incidents;
    }

    public async Task<Incident?> GetIncidentByIdAsync(string number)
    {
        return null; // still a stub — next after this
    }

    public async Task UpdateIncidentStatusAsync(string number, string status)
    {
        await Task.CompletedTask; // still a stub — next after this
    }

    // Plain string fields (e.g. sys_id, number) just return a raw string value
    private static string GetString(JsonElement item, string field)
    {
        if (!item.TryGetProperty(field, out var prop)) return string.Empty;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : "";
    }

    // With sysparm_display_value=all, reference/choice fields come back as
    // { "value": "...", "display_value": "..." } instead of a plain string
    private static string GetDisplayValue(JsonElement item, string field)
    {
        if (!item.TryGetProperty(field, out var prop)) return string.Empty;

        if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("display_value", out var dv))
            return dv.GetString() ?? "";

        return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? "" : "";
    }
}