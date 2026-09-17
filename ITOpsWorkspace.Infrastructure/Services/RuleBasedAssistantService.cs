using ITOpsWorkspace.Core.Enums;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Infrastructure.Services;

public class RuleBasedAssistantService : IAssistantService
{
    private readonly IIncidentSource _incidentSource;
    private readonly IPlaybookService _playbookService;

    public RuleBasedAssistantService(IIncidentSource incidentSource, IPlaybookService playbookService)
    {
        _incidentSource = incidentSource;
        _playbookService = playbookService;
    }

    public async Task<AssistantMessage> AskAsync(string incidentTitle, string incidentDescription,
        List<AssistantMessage> history, string userMessage)
    {
        var isInitialGreeting = string.IsNullOrWhiteSpace(userMessage);

        var keywords = isInitialGreeting
            ? incidentTitle
            : $"{incidentTitle} {userMessage}";

        var articles = await _incidentSource.SearchKnowledgeArticlesAsync(keywords);
        var playbooks = await _playbookService.SearchPlaybooksAsync(keywords);

        var suggestions = new List<AssistantSuggestion>();

        foreach (var article in articles.Take(3))
        {
            suggestions.Add(new AssistantSuggestion
            {
                Type = AssistantSuggestionType.KnowledgeArticle,
                Label = $"📘 {article.ShortDescription}",
                Url = article.Url
            });
        }

        foreach (var playbook in playbooks.Take(3))
        {
            suggestions.Add(new AssistantSuggestion
            {
                Type = AssistantSuggestionType.Playbook,
                Label = $"🧭 {playbook.Title}",
                Playbook = playbook
            });
        }

        var searchQuery = Uri.EscapeDataString(keywords);
        suggestions.Add(new AssistantSuggestion
        {
            Type = AssistantSuggestionType.WebSearch,
            Label = $"🌐 Search web: {keywords}",
            Url = $"https://www.google.com/search?q={searchQuery}"
        });

        string responseText;
        if (isInitialGreeting)
        {
            responseText = suggestions.Count > 1
                ? $"I see this incident is about \"{incidentTitle}\". Here's what I found that may help:"
                : $"I see this incident is about \"{incidentTitle}\". Let me know what you've already tried, or try a web search below.";
        }
        else
        {
            responseText = suggestions.Count > 1
                ? "Based on that, here's what I found:"
                : "Here's what I could find — you may also want to try a web search:";
        }

        return new AssistantMessage
        {
            Role = AssistantRole.Assistant,
            Text = responseText,
            Timestamp = DateTime.UtcNow,
            Suggestions = suggestions
        };
    }
}