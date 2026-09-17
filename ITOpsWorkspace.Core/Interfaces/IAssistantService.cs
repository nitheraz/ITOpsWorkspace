using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IAssistantService
{
    Task<AssistantMessage> AskAsync(string incidentTitle, string incidentDescription,
        List<AssistantMessage> history, string userMessage);
}