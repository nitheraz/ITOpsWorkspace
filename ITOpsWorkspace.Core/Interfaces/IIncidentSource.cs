using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IIncidentSource
{
    Task<List<Incident>> GetIncidentsAsync(IncidentQuery? query = null);
    Task<int> GetIncidentCountAsync(IncidentQuery query);
    Task<Incident?> GetIncidentByIdAsync(string number);
    Task UpdateIncidentStatusAsync(string number, string status);
}