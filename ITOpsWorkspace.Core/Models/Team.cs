namespace ITOpsWorkspace.Core.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<User> Members { get; set; } = new();
}