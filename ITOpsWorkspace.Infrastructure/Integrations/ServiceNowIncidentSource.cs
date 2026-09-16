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
    private readonly string _instanceUrl;

    public ServiceNowIncidentSource(HttpClient client, ServiceNowOptions options)
    {
        _client = client;
        _instanceUrl = options.InstanceUrl.TrimEnd('/');
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

        var queryString = BuildQueryString(query);
        if (queryString is not null)
            url += $"&sysparm_query={queryString}";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("result");

        var incidents = new List<Incident>();
        foreach (var item in results.EnumerateArray())
            incidents.Add(MapIncident(item));

        return incidents
            .OrderBy(i => (int)i.Priority == 0 ? int.MaxValue : (int)i.Priority)
            .ThenBy(i => i.CreatedAt)
            .ToList();
    }

    public async Task<int> GetIncidentCountAsync(IncidentQuery query)
    {
        var url = "/api/now/stats/incident?sysparm_count=true";

        var queryString = BuildQueryString(query);
        if (queryString is not null)
            url += $"&sysparm_query={queryString}";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var result = doc.RootElement.GetProperty("result");
        if (!result.TryGetProperty("stats", out var stats) || !stats.TryGetProperty("count", out var countProp))
            return 0;

        return countProp.ValueKind switch
        {
            JsonValueKind.String when int.TryParse(countProp.GetString(), out var parsed) => parsed,
            JsonValueKind.Number => countProp.GetInt32(),
            _ => 0
        };
    }

    public async Task<Incident?> GetIncidentBySysIdAsync(string sysId)
    {
        var fields = "sys_id,number,short_description,description,priority,state,assigned_to,caller_id,sys_created_on";
        var url = $"/api/now/table/incident/{sysId}?sysparm_display_value=all&sysparm_fields={fields}";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("result");

        return MapIncident(result);
    }

    public async Task<List<IncidentStateOption>> GetIncidentStateOptionsAsync()
    {
        var queryClause = HttpUtility.UrlEncode("name=incident^element=state^inactive=false") + "^ORDERBYsequence";
        var url = $"/api/now/table/sys_choice?sysparm_query={queryClause}&sysparm_fields=value,label&sysparm_limit=20";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("result");

        var options = new List<IncidentStateOption>();
        foreach (var item in results.EnumerateArray())
        {
            options.Add(new IncidentStateOption
            {
                Value = GetRawValue(item, "value"),
                Label = GetRawValue(item, "label")
            });
        }
        return options;
    }

    public Task AddWorkNoteAsync(string sysId, string note) =>
        PatchIncidentAsync(sysId, new Dictionary<string, string> { ["work_notes"] = note });

    public Task AssignToUserAsync(string sysId, string displayName) =>
        PatchIncidentAsync(sysId, new Dictionary<string, string> { ["assigned_to"] = displayName }, inputDisplayValue: true);

    public Task UpdateStateAsync(string sysId, string stateValue) =>
        PatchIncidentAsync(sysId, new Dictionary<string, string> { ["state"] = stateValue });

    public async Task<List<ActivityEntry>> GetActivityAsync(string incidentSysId)
    {
        var fields = "element,value,sys_created_on,sys_created_by";
        var queryClause = HttpUtility.UrlEncode($"element_id={incidentSysId}") + "^ORDERBYDESCsys_created_on";
        var url = $"/api/now/table/sys_journal_field?sysparm_query={queryClause}&sysparm_fields={fields}&sysparm_limit=50";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("result");

        var entries = new List<ActivityEntry>();
        foreach (var item in results.EnumerateArray())
        {
            var element = GetRawValue(item, "element");
            entries.Add(new ActivityEntry
            {
                Type = element switch
                {
                    "work_notes" => "Work Note",
                    "comments" => "Comment",
                    _ => element
                },
                Text = GetRawValue(item, "value"),
                Author = GetRawValue(item, "sys_created_by"),
                Timestamp = DateTime.TryParse(GetRawValue(item, "sys_created_on"), out var dt) ? dt : DateTime.MinValue
            });
        }

        return entries;
    }

    public async Task<List<KnowledgeArticle>> SearchKnowledgeArticlesAsync(string keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords)) return new List<KnowledgeArticle>();

        var queryClause = "short_descriptionLIKE" + HttpUtility.UrlEncode(keywords) +
                           "^workflow_state=published^active=true";
        var url = $"/api/now/table/kb_knowledge?sysparm_query={queryClause}" +
                   "&sysparm_fields=number,short_description&sysparm_limit=5";

        HttpResponseMessage response;
        try
        {
            response = await _client.GetAsync(url);
        }
        catch
        {
            return new List<KnowledgeArticle>();
        }

        if (!response.IsSuccessStatusCode) return new List<KnowledgeArticle>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = doc.RootElement.GetProperty("result");

        var articles = new List<KnowledgeArticle>();
        foreach (var item in results.EnumerateArray())
        {
            var number = GetRawValue(item, "number");
            articles.Add(new KnowledgeArticle
            {
                Number = number,
                ShortDescription = GetRawValue(item, "short_description"),
                Url = $"{_instanceUrl}/kb_view.do?sysparm_article={number}"
            });
        }
        return articles;
    }

    private async Task PatchIncidentAsync(string sysId, Dictionary<string, string> fields, bool inputDisplayValue = false)
    {
        var url = $"/api/now/table/incident/{sysId}";
        if (inputDisplayValue)
            url += "?sysparm_input_display_value=true";

        var json = JsonSerializer.Serialize(fields);
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static Incident MapIncident(JsonElement item) => new Incident
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
    };

    private static string? BuildQueryString(IncidentQuery query)
    {
        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.AssignedToName))
            clauses.Add($"assigned_to.name={HttpUtility.UrlEncode(query.AssignedToName)}");

        if (!string.IsNullOrWhiteSpace(query.AssignmentGroupName))
            clauses.Add($"assignment_group.name={HttpUtility.UrlEncode(query.AssignmentGroupName)}");

        if (query.AssignedToIsEmpty)
            clauses.Add("assigned_toISEMPTY");

        if (!string.IsNullOrWhiteSpace(query.PriorityValue))
            clauses.Add($"priority={query.PriorityValue}");

        return clauses.Count > 0 ? string.Join("^", clauses) : null;
    }

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