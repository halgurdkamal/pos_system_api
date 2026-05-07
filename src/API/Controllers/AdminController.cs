using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Admin.Queries.GetDatabaseStats;
using pos_system_api.Infrastructure.Data.Seeders;

namespace pos_system_api.API.Controllers;

/// <summary>
/// Admin endpoints for database management and diagnostics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly DatabaseSeeder _databaseSeeder;
    private readonly UserSeeder _userSeeder;

    public AdminController(
        IMediator mediator,
        DatabaseSeeder databaseSeeder,
        UserSeeder userSeeder)
    {
        _mediator = mediator;
        _databaseSeeder = databaseSeeder;
        _userSeeder = userSeeder;
    }

    /// <summary>Seed database with sample data.</summary>
    [HttpPost("seed")]
    [AllowAnonymous] // Temporarily allow anonymous for seeding during development
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> SeedDatabase()
    {
        await _databaseSeeder.SeedAllAsync();
        return Ok(new { message = "Database seeded successfully" });
    }

    /// <summary>Clear all seeded data from the database.</summary>
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ClearDatabase()
    {
        await _databaseSeeder.ClearAllAsync();
        return Ok(new { message = "Database cleared successfully" });
    }

    /// <summary>Seed initial user accounts (dev/test only).</summary>
    [HttpPost("seed-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> SeedUsers()
    {
        await _userSeeder.SeedAsync();
        return Ok(new { message = "Users seeded successfully. Check console output for login credentials." });
    }

    /// <summary>Get row counts for major tables.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DatabaseStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseStatsDto>> GetDatabaseStats()
    {
        var stats = await _mediator.Send(new GetDatabaseStatsQuery());
        return Ok(stats);
    }
}
