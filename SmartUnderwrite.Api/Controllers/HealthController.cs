using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Infrastructure.Data;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(SmartUnderwriteDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("healthz")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();
            
            _logger.LogInformation("Health check passed");
            
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }

    [HttpGet("readyz")]
    public async Task<IActionResult> ReadinessCheck()
    {
        try
        {
            // More comprehensive readiness check
            await _context.Database.CanConnectAsync();
            
            // Check if database has been migrated (basic check)
            var canQuery = await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            
            _logger.LogInformation("Readiness check passed");
            
            return Ok(new
            {
                Status = "Ready",
                Timestamp = DateTime.UtcNow,
                DatabaseConnected = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            
            return StatusCode(503, new
            {
                Status = "Not Ready",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }
}