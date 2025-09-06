using FlightBooking.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseTestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DatabaseTestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            // Test database connection
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return BadRequest(new { 
                    Status = "Failed", 
                    Message = "Cannot connect to database",
                    ConnectionString = _context.Database.GetConnectionString()?.Replace("Password=postgres", "Password=***")
                });
            }

            // Get database info
            var databaseName = _context.Database.GetDbConnection().Database;
            var serverVersion = await _context.Database.ExecuteSqlRawAsync("SELECT version()");
            
            return Ok(new { 
                Status = "Connected", 
                Database = databaseName,
                Message = "Successfully connected to PostgreSQL",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("migrate")]
    public async Task<IActionResult> RunMigrations()
    {
        try
        {
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();

            if (pendingMigrations.Any())
            {
                await _context.Database.MigrateAsync();
                return Ok(new { 
                    Status = "Success", 
                    Message = "Migrations applied successfully",
                    PendingMigrations = pendingMigrations.ToList(),
                    AppliedMigrations = appliedMigrations.ToList()
                });
            }

            return Ok(new { 
                Status = "Up to date", 
                Message = "No pending migrations",
                AppliedMigrations = appliedMigrations.ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        try
        {
            var tables = await _context.Database.SqlQueryRaw<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'"
            ).ToListAsync();

            return Ok(new { 
                Status = "Success", 
                Tables = tables,
                Count = tables.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message
            });
        }
    }
}
