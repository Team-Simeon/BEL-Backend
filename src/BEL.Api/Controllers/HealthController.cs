using BEL.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEL.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class HealthController(BelDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var databaseReachable = await db.Database.CanConnectAsync(cancellationToken);
        var result = new { status = databaseReachable ? "Healthy" : "Unhealthy", database = databaseReachable };

        return databaseReachable ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
