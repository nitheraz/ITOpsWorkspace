namespace ITOpsWorkspace.Core.Models;

public class Organisation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<AppUser> Users { get; set; } = new();
}