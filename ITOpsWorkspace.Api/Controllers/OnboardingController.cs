using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ITOpsWorkspace.Api.Data;
using ITOpsWorkspace.Api.Dtos;
using ITOpsWorkspace.Core.Enums;
using ITOpsWorkspace.Core.Models;
using Microsoft.AspNetCore.Authorization;

namespace ITOpsWorkspace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public OnboardingController(ApiDbContext db)
    {
        _db = db;
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupOrganisationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganisationName) ||
            string.IsNullOrWhiteSpace(request.AdminName) ||
            string.IsNullOrWhiteSpace(request.AdminEmail))
        {
            return BadRequest("Organisation name, admin name, and admin email are all required.");
        }

        var organisation = new Organisation
        {
            Name = request.OrganisationName
        };

        var user = new AppUser
        {
            OrganisationId = organisation.Id,
            Name = request.AdminName,
            Email = request.AdminEmail.Trim().ToLowerInvariant(),
            Role = UserRole.Administrator,
            Status = UserStatus.PendingInvitation
        };

        var invitation = new Invitation
        {
            UserId = user.Id,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Organisations.Add(organisation);
        _db.Users.Add(user);
        _db.Invitations.Add(invitation);

        await _db.SaveChangesAsync();

        return Ok(new SetupOrganisationResponse
        {
            OrganisationId = organisation.Id,
            UserId = user.Id,
            InvitationToken = invitation.Token,
            ExpiresAt = invitation.ExpiresAt
        });
    }

    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Token and password are required.");
        }

        if (request.Password.Length < 8)
        {
            return BadRequest("Password must be at least 8 characters.");
        }

        var invitation = await _db.Invitations
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Token == request.Token);

        if (invitation is null)
            return NotFound("Invitation not found.");

        if (invitation.AcceptedAt is not null)
            return BadRequest("This invitation has already been used.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            return BadRequest("This invitation has expired.");

        var user = invitation.User;
        if (user is null)
            return NotFound("The user for this invitation no longer exists.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.Status = UserStatus.Active;

        invitation.AcceptedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new AcceptInvitationResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name
        });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("invite-user")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest("Name, email, and role are all required.");
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest("Role must be one of: Administrator, ITManager, Technician, ReadOnly.");
        }

        var orgIdClaim = User.FindFirst("org")?.Value;
        if (!Guid.TryParse(orgIdClaim, out var organisationId))
            return Unauthorized();

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("A user with this email already exists.");

        var user = new AppUser
        {
            OrganisationId = organisationId,
            Name = request.Name,
            Email = email,
            Role = role,
            Status = UserStatus.PendingInvitation
        };

        var invitation = new Invitation
        {
            UserId = user.Id,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Users.Add(user);
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync();

        return Ok(new InviteUserResponse
        {
            UserId = user.Id,
            InvitationToken = invitation.Token,
            ExpiresAt = invitation.ExpiresAt
        });
    }
    private static string GenerateToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}