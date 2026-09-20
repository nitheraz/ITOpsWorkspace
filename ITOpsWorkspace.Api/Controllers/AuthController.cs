using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ITOpsWorkspace.Api.Data;
using ITOpsWorkspace.Api.Dtos;
using ITOpsWorkspace.Core.Enums;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public AuthController(ApiDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || user.PasswordHash is null)
            return Unauthorized("Invalid email or password.");

        if (user.Status != UserStatus.Active)
            return Unauthorized("This account is not active.");

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var (token, expiresAt) = GenerateJwt(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            OrganisationId = user.OrganisationId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString()
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var name = User.FindFirst("name")?.Value;
        var role = User.FindFirst("role")?.Value;
        var org = User.FindFirst("org")?.Value;

        return Ok(new
        {
            userId,
            email,
            name,
            role,
            organisationId = org
        });
    }

    private (string Token, DateTime ExpiresAt) GenerateJwt(AppUser user)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "ITOpsWorkspace.Api";
        var audience = _config["Jwt:Audience"] ?? "ITOpsWorkspace.App";
        var minutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var m) ? m : 15;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Name),
            new("role", user.Role.ToString()),
            new("org", user.OrganisationId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}