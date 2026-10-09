using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ITOpsWorkspace.Api.Data;
using ITOpsWorkspace.Api.Dtos;

namespace ITOpsWorkspace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApiDbContext _db;

    public UsersController(ApiDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,ITManager")]
    public async Task<IActionResult> GetUsers()
    {
        var orgIdClaim = User.FindFirst("org")?.Value;
        if (!Guid.TryParse(orgIdClaim, out var organisationId))
            return Unauthorized();

        var users = await _db.Users
            .Where(u => u.OrganisationId == organisationId)
            .Select(u => new UserSummaryResponse
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        return Ok(users);
    }
}