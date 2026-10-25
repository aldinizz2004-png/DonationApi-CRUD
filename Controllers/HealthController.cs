using DonationApi.Data;
using Microsoft.AspNetCore.Mvc;

namespace DonationApi.Controllers;

[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    /// GET api/health
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get()
    {
        var dbOk = await _db.Database.CanConnectAsync();

        var body = new
        {
            status = dbOk ? "Healthy" : "Unhealthy",
            database = dbOk ? "Connected" : "Unreachable",
            timestamp = DateTime.UtcNow
        };

        return dbOk ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}