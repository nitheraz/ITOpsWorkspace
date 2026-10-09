using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IIncidentSource
{
    Task<List<Incident>> GetIncidentsAsync(IncidentQuery? query = null);
    Task<int> GetIncidentCountAsync(IncidentQuery query);
    Task<Incident?> GetIncidentBySysIdAsync(string sysId);
    Task<List<IncidentStateOption>> GetIncidentStateOptionsAsync();

    Task<List<IncidentResolutionCode>> GetIncidentResolutionCodesAsync();

    Task ResolveIncidentAsync(
        string sysId,
        string stateValue,
        string resolutionCode,
        string resolutionNotes);

    Task AddWorkNoteAsync(string sysId, string note);
    Task AssignToUserAsync(string sysId, string displayName);
    Task UpdateStateAsync(string sysId, string stateValue);
    Task<List<ActivityEntry>> GetActivityAsync(string incidentSysId);
    Task<List<KnowledgeArticle>> SearchKnowledgeArticlesAsync(string keywords);
}