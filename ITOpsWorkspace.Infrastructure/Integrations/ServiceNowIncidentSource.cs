using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;
using ITOpsWorkspace.Core.Services;

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

    public async Task<List<Incident>> GetIncidentsAsync(IncidentQuery? query = null)
    {
        query ??= new IncidentQuery();

        var fields = "sys_id,number,short_description,description,priority,state,assigned_to,caller_id,sys_created_on";
        var url = $"/api/now/table/incident?sysparm_limit={query.Limit}" +
                   "&sysparm_display_value=all" +
                   $"&sysparm_fields={fields}";

        var clauses = new List<string>();

        if (query.IncludeUnassigned)
            clauses.Add("assigned_toISEMPTY");

        if (!string.IsNullOrWhiteSpace(query.AssignedToName))
            clauses.Add($"assigned_to.name={HttpUtility.UrlEncode(query.AssignedToName)}");

        foreach (var name in query.AssignedToNames)
            clauses.Add($"assigned_to.name={HttpUtility.UrlEncode(name)}");

        if (clauses.Count > 0)
            url += "&sysparm_query=" + string.Join("^OR", clauses);

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
                ServiceNowSysId = GetRawValue(item, "sys_id"),
                Number = GetRawValue(item, "number"),
                Title = GetDisplayValue(item, "short_description"),
                Description = GetRawValue(item, "description"),
                Priority = PriorityParser.Parse(GetRawValue(item, "priority")),
                PriorityDisplay = GetDisplayValue(item, "priority"),
                StateDisplay = GetDisplayValue(item, "state"),
                RequesterName = GetDisplayValue(item, "caller_id"),
                AssignedToName = GetDisplayValue(item, "assigned_to"),
                CreatedAt = DateTime.TryParse(GetRawValue(item, "sys_created_on"), out var dt) ? dt : DateTime.MinValue
            });
        }

        return incidents
            .OrderBy(i => (int)i.Priority == 0 ? int.MaxValue : (int)i.Priority)
            .ThenBy(i => i.CreatedAt)
            .ToList();
    }

    public Task<Incident?> GetIncidentByIdAsync(string number) => Task.FromResult<Incident?>(null);
    public Task UpdateIncidentStatusAsync(string number, string status) => Task.CompletedTask;

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