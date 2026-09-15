namespace ITOpsWorkspace.Core.Models;

public class TeamContext
{
    public List<string> MemberNames { get; }

    public TeamContext(List<string> memberNames)
    {
        MemberNames = memberNames;
    }
}