using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartUnderwrite.Api.Attributes;
using SmartUnderwrite.Api.Constants;
using SmartUnderwrite.Api.Models.Audit;
using SmartUnderwrite.Api.Services;
using SmartUnderwrite.Core.Entities;

namespace SmartUnderwrite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with optional filtering
    /// </summary>
    /// <param name="request">Filter parameters</param>
    /// <returns>List of audit logs</returns>
    [HttpGet]
    [AuthorizeRoles(Roles.Admin, Roles.Underwriter)]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs([FromQuery] AuditLogFilterRequest request)
    {
        try
        {
            _logger.LogInformation("Retrieving audit logs with filters: EntityType={EntityType}, EntityId={EntityId}, FromDate={FromDate}, ToDate={ToDate}",
                request.EntityType, request.EntityId, request.FromDate, request.ToDate);

            var auditLogs = await _auditService.GetAuditLogsAsync(
                request.EntityType,
                request.EntityId,
                request.FromDate,
                request.ToDate);

            var auditLogDtos = auditLogs.Select(MapToDto).ToList();

            _logger.LogInformation("Retrieved {Count} audit logs", auditLogDtos.Count);

            return Ok(auditLogDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, "An error occurred while retrieving audit logs");
        }
    }

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    /// <param name="entityType">Type of entity (e.g., LoanApplication, Rule)</param>
    /// <param name="entityId">ID of the entity</param>
    /// <returns>Chronological audit trail for the entity</returns>
    [HttpGet("trail/{entityType}/{entityId}")]
    [AuthorizeRoles(Roles.Admin, Roles.Underwriter)]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetEntityAuditTrail(string entityType, string entityId)
    {
        try
        {
            _logger.LogInformation("Retrieving audit trail for {EntityType} {EntityId}", entityType, entityId);

            var auditTrail = await _auditService.GetEntityAuditTrailAsync(entityType, entityId);
            var auditTrailDtos = auditTrail.Select(MapToDto).ToList();

            _logger.LogInformation("Retrieved audit trail for {EntityType} {EntityId}: {Count} entries",
                entityType, entityId, auditTrailDtos.Count);

            return Ok(auditTrailDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while retrieving the audit trail");
        }
    }

    /// <summary>
    /// Get a specific audit log entry
    /// </summary>
    /// <param name="id">Audit log ID</param>
    /// <returns>Audit log details</returns>
    [HttpGet("{id:int}")]
    [AuthorizeRoles(Roles.Admin, Roles.Underwriter)]
    public async Task<ActionResult<AuditLogDto>> GetAuditLog(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving audit log {Id}", id);

            var auditLog = await _auditService.GetAuditLogAsync(id);
            if (auditLog == null)
            {
                _logger.LogWarning("Audit log {Id} not found", id);
                return NotFound($"Audit log with ID {id} not found");
            }

            var auditLogDto = MapToDto(auditLog);
            return Ok(auditLogDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the audit log");
        }
    }

    /// <summary>
    /// Get audit summary statistics
    /// </summary>
    /// <param name="fromDate">Start date for statistics</param>
    /// <param name="toDate">End date for statistics</param>
    /// <returns>Audit statistics summary</returns>
    [HttpGet("summary")]
    [AuthorizeRoles(Roles.Admin)]
    public async Task<ActionResult<AuditSummaryDto>> GetAuditSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            _logger.LogInformation("Retrieving audit summary from {FromDate} to {ToDate}", fromDate, toDate);

            var auditLogs = await _auditService.GetAuditLogsAsync(
                fromDate: fromDate,
                toDate: toDate);

            var summary = new AuditSummaryDto
            {
                TotalEntries = auditLogs.Count(),
                DateRange = new DateRangeDto
                {
                    FromDate = fromDate,
                    ToDate = toDate
                },
                ActionCounts = auditLogs
                    .GroupBy(a => a.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EntityTypeCounts = auditLogs
                    .GroupBy(a => a.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                UserActivityCounts = auditLogs
                    .Where(a => !string.IsNullOrEmpty(a.UserId))
                    .GroupBy(a => a.UserId!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            _logger.LogInformation("Generated audit summary: {TotalEntries} entries", summary.TotalEntries);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit summary");
            return StatusCode(500, "An error occurred while generating the audit summary");
        }
    }

    private static AuditLogDto MapToDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            Action = auditLog.Action,
            Changes = auditLog.Changes,
            UserId = auditLog.UserId,
            Timestamp = auditLog.Timestamp
        };
    }
}