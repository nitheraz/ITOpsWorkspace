namespace ITOpsWorkspace.Core.Models;

public class CurrentUserContext
{
    public string DisplayName { get; }

    public CurrentUserContext(string displayName)
    {
        DisplayName = displayName;
    }
}