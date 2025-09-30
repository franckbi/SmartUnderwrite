using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartUnderwrite.Api.Attributes;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Infrastructure.Data;
using System.Diagnostics;
using System.Reflection;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly SmartUnderwriteDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        SmartUnderwriteDbContext context, 
        ICurrentUserService currentUserService,
        ILogger<HealthController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint for load balancers and monitoring systems
    /// </summary>
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
                Version = GetApplicationVersion(),
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

    /// <summary>
    /// Readiness check endpoint to verify the application is ready to serve traffic
    /// </summary>
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
                DatabaseConnected = true,
                Version = GetApplicationVersion()
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

    [HttpGet("protected")]
    [AffiliateAccess]
    public IActionResult ProtectedEndpoint()
    {
        var userId = _currentUserService.GetUserId();
        var affiliateId = _currentUserService.GetAffiliateId();
        var roles = _currentUserService.GetRoles();

        _logger.LogInformation("Protected endpoint accessed by user {UserId} with roles [{Roles}]", 
            userId, string.Join(", ", roles));

        return Ok(new
        {
            Message = "Access granted to protected endpoint",
            UserId = userId,
            AffiliateId = affiliateId,
            Roles = roles,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("admin-only")]
    [AdminOnly]
    public IActionResult AdminOnlyEndpoint()
    {
        var userId = _currentUserService.GetUserId();
        var roles = _currentUserService.GetRoles();

        _logger.LogInformation("Admin-only endpoint accessed by user {UserId}", userId);

        return Ok(new
        {
            Message = "Access granted to admin-only endpoint",
            UserId = userId,
            Roles = roles,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Comprehensive system metrics and status information
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(Environment.TickCount);

            // Database metrics
            var dbMetrics = await GetDatabaseMetricsAsync();

            var metrics = new
            {
                Timestamp = DateTime.UtcNow,
                Application = new
                {
                    Name = "SmartUnderwrite API",
                    Version = GetApplicationVersion(),
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    StartTime = startTime,
                    Uptime = DateTime.UtcNow - startTime
                },
                System = new
                {
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    OSVersion = Environment.OSVersion.ToString(),
                    WorkingSet = process.WorkingSet64,
                    PrivateMemorySize = process.PrivateMemorySize64,
                    ThreadCount = process.Threads.Count
                },
                Database = dbMetrics
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, new { message = "Error retrieving metrics", error = ex.Message });
        }
    }

    /// <summary>
    /// Application information endpoint
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetApplicationInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buildDate = GetBuildDate(assembly);

        return Ok(new
        {
            Application = new
            {
                Name = "SmartUnderwrite API",
                Description = "Automated loan underwriting system with configurable rules engine",
                Version = GetApplicationVersion(),
                BuildDate = buildDate,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            },
            Features = new
            {
                Authentication = "JWT Bearer Token",
                Authorization = "Role-based (Admin, Underwriter, Affiliate)",
                Database = "PostgreSQL with Entity Framework Core",
                Storage = "MinIO/S3 for document storage",
                RulesEngine = "JSON-based configurable rules",
                AuditLogging = "Comprehensive audit trail",
                HealthChecks = "Kubernetes-compatible health endpoints"
            },
            Endpoints = new
            {
                Health = "/api/health/healthz",
                Readiness = "/api/health/readyz",
                Metrics = "/api/health/metrics",
                OpenAPI = "/openapi/v1.json",
                SwaggerUI = "/swagger"
            }
        });
    }

    /// <summary>
    /// Detailed health check with component status
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> DetailedHealthCheck()
    {
        var checks = new Dictionary<string, object>();
        var overallStatus = "Healthy";

        // Database check
        try
        {
            await _context.Database.CanConnectAsync();
            var dbMetrics = await GetDatabaseMetricsAsync();
            checks["Database"] = new { Status = "Healthy", Metrics = dbMetrics };
        }
        catch (Exception ex)
        {
            checks["Database"] = new { Status = "Unhealthy", Error = ex.Message };
            overallStatus = "Unhealthy";
        }

        // Memory check
        var process = Process.GetCurrentProcess();
        var memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
        var memoryStatus = memoryUsageMB > 1000 ? "Warning" : "Healthy"; // Warning if over 1GB
        if (memoryStatus == "Warning" && overallStatus == "Healthy")
        {
            overallStatus = "Warning";
        }

        checks["Memory"] = new
        {
            Status = memoryStatus,
            WorkingSetMB = memoryUsageMB,
            PrivateMemoryMB = process.PrivateMemorySize64 / (1024 * 1024)
        };

        // Thread count check
        var threadCount = process.Threads.Count;
        var threadStatus = threadCount > 100 ? "Warning" : "Healthy"; // Warning if over 100 threads
        if (threadStatus == "Warning" && overallStatus == "Healthy")
        {
            overallStatus = "Warning";
        }

        checks["Threads"] = new
        {
            Status = threadStatus,
            Count = threadCount
        };

        var statusCode = overallStatus switch
        {
            "Healthy" => 200,
            "Warning" => 200,
            "Unhealthy" => 503,
            _ => 500
        };

        return StatusCode(statusCode, new
        {
            Status = overallStatus,
            Timestamp = DateTime.UtcNow,
            Version = GetApplicationVersion(),
            Checks = checks
        });
    }

    private async Task<object> GetDatabaseMetricsAsync()
    {
        try
        {
            var affiliateCount = await _context.Affiliates.CountAsync();
            var userCount = await _context.Users.CountAsync();
            var applicationCount = await _context.LoanApplications.CountAsync();
            var ruleCount = await _context.Rules.CountAsync();
            var activeRuleCount = await _context.Rules.CountAsync(r => r.IsActive);

            return new
            {
                Status = "Connected",
                Affiliates = affiliateCount,
                Users = userCount,
                Applications = applicationCount,
                Rules = ruleCount,
                ActiveRules = activeRuleCount
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Status = "Error",
                Error = ex.Message
            };
        }
    }

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        const string BuildVersionMetadataPrefix = "+build";
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(BuildVersionMetadataPrefix);
            if (index > 0)
            {
                value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                if (DateTime.TryParse(value, out var result))
                    return result;
            }
        }

        return new FileInfo(assembly.Location).CreationTime;
    }
}