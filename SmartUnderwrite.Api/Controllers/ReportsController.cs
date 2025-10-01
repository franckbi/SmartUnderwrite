using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartUnderwrite.Api.Models.Reports;
using SmartUnderwrite.Api.Services;
using System.Security.Claims;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportsService reportsService, ILogger<ReportsController> logger)
    {
        _reportsService = reportsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets dashboard report data for the specified date range
    /// </summary>
    /// <param name="fromDate">Start date for the report (optional)</param>
    /// <param name="toDate">End date for the report (optional)</param>
    /// <returns>Dashboard report data</returns>
    [HttpGet("dashboard")]
    [Authorize(Policy = "UnderwriterOrAdmin")]
    public async Task<ActionResult<ReportDataDto>> GetDashboardReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            // Default to last 30 days if no dates provided
            var startDate = fromDate.HasValue 
                ? DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow.AddDays(-30);
            var endDate = toDate.HasValue 
                ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow;

            _logger.LogInformation("Getting dashboard report from {StartDate} to {EndDate}", startDate, endDate);
            
            var reportData = await _reportsService.GetDashboardReportAsync(startDate, endDate, User);
            return Ok(reportData);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized access: {Message}", ex.Message);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard report");
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }
}