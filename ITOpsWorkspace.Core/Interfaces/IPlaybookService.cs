using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IPlaybookService
{
    Task<List<Playbook>> GetAllPlaybooksAsync();

    Task<Playbook?> GetPlaybookAsync(int id);

    Task SavePlaybookAsync(Playbook playbook);

    Task DeletePlaybookAsync(int id);

    Task<List<Playbook>> SearchPlaybooksAsync(string keywords);

    Task<PlaybookRun> StartRunAsync(
        int playbookId,
        string incidentSysId,
        string incidentNumber);

    // Existing method retained for compatibility.
    Task<PlaybookRun?> GetActiveRunAsync(string incidentSysId);

    // Returns all active playbook runs for an incident.
    Task<List<PlaybookRun>> GetActiveRunsAsync(string incidentSysId);

    // Returns the active run for a specific playbook and incident.
    Task<PlaybookRun?> GetActiveRunAsync(
        string incidentSysId,
        int playbookId);

    Task SetStepCheckedAsync(
        int runStepStateId,
        bool isChecked);

    Task CompleteRunAsync(int runId);
}