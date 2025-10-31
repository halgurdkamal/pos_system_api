using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Infrastructure.Data;
using pos_system_api.Infrastructure.Data.Seeders;
using pos_system_api.Infrastructure.Auth;

namespace pos_system_api.API.Controllers;

/// <summary>
/// Admin controller for database management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Seed database with sample data
    /// </summary>
    /// <returns>Seeding result</returns>
    [HttpPost("seed")]
    [AllowAnonymous] // Temporarily allow anonymous for seeding during development
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SeedDatabase()
    {
        try
        {
            var seeder = new DatabaseSeeder(_context);
            await seeder.SeedAllAsync();
            return Ok(new { message = "Database seeded successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Seeding failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Clear all seeded data from database
    /// </summary>
    /// <returns>Clear result</returns>
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ClearDatabase()
    {
        try
        {
            var seeder = new DatabaseSeeder(_context);
            await seeder.ClearAllAsync();
            return Ok(new { message = "Database cleared successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Clear failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Seed user accounts for testing
    /// </summary>
    /// <returns>Seeding result</returns>
    [HttpPost("seed-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SeedUsers()
    {
        try
        {
            var passwordHasher = new PasswordHasher();
            var userSeeder = new UserSeeder(_context, passwordHasher);
            await userSeeder.SeedAsync();
            return Ok(new { message = "Users seeded successfully. Check console output for login credentials." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "User seeding failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Get database statistics
    /// </summary>
    /// <returns>Count of entities in database</returns>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetDatabaseStats()
    {
        var stats = new
        {
            drugs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.Drugs),
            shops = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.Shops),
            suppliers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.Suppliers),
            inventory = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.ShopInventory),
            users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.Users),
            timestamp = DateTime.UtcNow
        };

        return Ok(stats);
    }
}
