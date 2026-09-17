using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IPlaybookService
{
    Task<List<Playbook>> GetAllPlaybooksAsync();
    Task<Playbook?> GetPlaybookAsync(int id);
    Task SavePlaybookAsync(Playbook playbook);
    Task DeletePlaybookAsync(int id);
    Task<List<Playbook>> SearchPlaybooksAsync(string keywords);

    Task<PlaybookRun> StartRunAsync(int playbookId, string incidentSysId, string incidentNumber);
    Task<PlaybookRun?> GetActiveRunAsync(string incidentSysId);
    Task SetStepCheckedAsync(int runStepStateId, bool isChecked);
    Task CompleteRunAsync(int runId);
}