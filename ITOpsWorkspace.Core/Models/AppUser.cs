using ITOpsWorkspace.Core.Enums;

namespace ITOpsWorkspace.Core.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Technician;
    public UserStatus Status { get; set; } = UserStatus.PendingInvitation;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}