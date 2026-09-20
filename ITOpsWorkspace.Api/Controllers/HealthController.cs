using Microsoft.AspNetCore.Mvc;
using ITOpsWorkspace.Api.Data;

namespace ITOpsWorkspace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApiDbContext _db;

    public HealthController(ApiDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var canConnect = await _db.Database.CanConnectAsync();
        return Ok(new { database = canConnect ? "connected" : "unreachable" });
    }
}