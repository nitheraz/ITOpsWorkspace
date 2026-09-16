namespace ITOpsWorkspace.Core.Models;

public class TeamContext
{
    public string AssignmentGroupName { get; }

    public TeamContext(string assignmentGroupName)
    {
        AssignmentGroupName = assignmentGroupName;
    }
}