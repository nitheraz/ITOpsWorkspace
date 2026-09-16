using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using ITOpsWorkspace.Core.Interfaces;

namespace ITOpsWorkspace.Infrastructure.Integrations;

public class ServiceNowUserLookupService : IUserLookupService
{
    public async Task<string?> GetDisplayNameAsync(string instanceUrl, string username, string password)
    {
        using var client = CreateClient(instanceUrl, username, password);

        var query = HttpUtility.UrlEncode($"user_name={username}");
        var url = $"/api/now/table/sys_user?sysparm_query={query}&sysparm_fields=name&sysparm_limit=1";

        var doc = await GetJsonAsync(client, url);
        if (doc is null) return null;

        var results = doc.RootElement.GetProperty("result");
        if (results.GetArrayLength() == 0) return null;

        return results[0].GetProperty("name").GetString();
    }

    public async Task<List<string>> GetAssignmentGroupNamesAsync(string instanceUrl, string username, string password)
    {
        using var client = CreateClient(instanceUrl, username, password);

        var query = HttpUtility.UrlEncode($"user.user_name={username}");
        var url = $"/api/now/table/sys_user_grmember?sysparm_query={query}" +
                  "&sysparm_fields=group&sysparm_display_value=all&sysparm_limit=25";

        var doc = await GetJsonAsync(client, url);
        if (doc is null) return new List<string>();

        var results = doc.RootElement.GetProperty("result");
        var groupNames = new List<string>();

        foreach (var item in results.EnumerateArray())
        {
            var groupField = item.GetProperty("group");
            if (groupField.ValueKind == JsonValueKind.Object && groupField.TryGetProperty("display_value", out var dv))
            {
                var name = dv.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    groupNames.Add(name);
            }
        }

        return groupNames;
    }

    private static HttpClient CreateClient(string instanceUrl, string username, string password)
    {
        var client = new HttpClient { BaseAddress = new Uri(instanceUrl) };
        var authBytes = Encoding.ASCII.GetBytes($"{username}:{password}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<JsonDocument?> GetJsonAsync(HttpClient client, string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url);
        }
        catch
        {
            return null;
        }

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }
}