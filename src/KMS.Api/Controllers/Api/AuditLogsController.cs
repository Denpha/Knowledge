using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Logging;
using KMS.Application.Interfaces;
using System.Text;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Faculty")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogService auditLogService,
        ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    // GET: api/auditlogs
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new AuditLogSearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending,
                SearchTerm = search,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _auditLogService.SearchAsync(searchParams, cancellationToken);
            return this.Ok(result, "Audit logs retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return this.InternalServerError<PaginatedResult<AuditLogDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auditlogs/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetAuditLog(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = await _auditLogService.GetByIdAsync(id, cancellationToken);

            if (auditLog == null)
            {
                return this.NotFound<AuditLogDto>($"Audit log with ID {id} not found.");
            }

            return this.Ok(auditLog, "Audit log retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log {AuditLogId}", id);
            return this.InternalServerError<AuditLogDto>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auditlogs/entity/{entityName}/{entityId}
    [HttpGet("entity/{entityName}/{entityId:guid}")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AuditLogDto>>>> GetEntityAuditLogs(
        string entityName,
        Guid entityId,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new AuditLogSearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = "CreatedAt",
                SortDescending = true,
                Action = action,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _auditLogService.GetEntityAuditLogsAsync(
                entityName,
                entityId,
                searchParams,
                cancellationToken);

            return this.Ok(result, $"Audit logs for {entityName}/{entityId} retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityName}/{EntityId}", entityName, entityId);
            return this.InternalServerError<PaginatedResult<AuditLogDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auditlogs/user/{userId}
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AuditLogDto>>>> GetUserAuditLogs(
        Guid userId,
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new AuditLogSearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = "CreatedAt",
                SortDescending = true,
                EntityName = entityName,
                Action = action,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _auditLogService.GetUserAuditLogsAsync(
                userId,
                searchParams,
                cancellationToken);

            return this.Ok(result, $"Audit logs for user {userId} retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            return this.InternalServerError<PaginatedResult<AuditLogDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auditlogs/summary
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<AuditSummaryDto>>> GetAuditSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var defaultFromDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var defaultToDate = toDate ?? DateTime.UtcNow;

            if (defaultFromDate > defaultToDate)
            {
                return this.BadRequest<AuditSummaryDto>("FromDate must be before ToDate.");
            }

            var summary = await _auditLogService.GetAuditSummaryAsync(
                defaultFromDate,
                defaultToDate,
                cancellationToken);

            return this.Ok(summary, "Audit summary retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit summary");
            return this.InternalServerError<AuditSummaryDto>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auditlogs/export/csv
    [HttpGet("export/csv")]
    public async Task<ActionResult> ExportToCsv(
        [FromQuery] string? entityName,
        [FromQuery] Guid? entityId,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new AuditLogSearchParams
            {
                SearchTerm = search,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var csvData = await _auditLogService.ExportToCsvAsync(searchParams, cancellationToken);

            var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(csvData, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs to CSV");
            return StatusCode(500, new ApiResponse($"Internal server error: {ex.Message}"));
        }
    }

    // POST: api/auditlogs/log
    [HttpPost("log")]
    [AllowAnonymous] // This endpoint might be called from middleware or background tasks
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> LogAuditEvent(
        [FromBody] LogAuditEventRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.EntityName))
            {
                return this.BadRequest<AuditLogDto>("EntityName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Action))
            {
                return this.BadRequest<AuditLogDto>("Action is required.");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var auditLog = await _auditLogService.LogAsync(
                request.EntityName,
                request.EntityId,
                request.Action,
                request.UserId,
                request.OldValues,
                request.NewValues,
                ipAddress,
                userAgent,
                cancellationToken);

            return this.Ok(auditLog, "Audit event logged successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit event");
            return this.InternalServerError<AuditLogDto>($"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/auditlogs/cleanup
    [HttpDelete("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> CleanupOldLogs(
        [FromQuery] int daysToKeep = 365,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (daysToKeep < 30)
            {
                return this.BadRequest<int>("daysToKeep must be at least 30 for audit logs.");
            }

            var deletedCount = await _auditLogService.CleanupOldLogsAsync(daysToKeep, cancellationToken);
            return this.Ok(deletedCount, $"Deleted {deletedCount} old audit logs.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old audit logs");
            return this.InternalServerError<int>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auditlogs/test
    [HttpGet("test")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> TestAuditLogs(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test logging an audit event
            var testAuditLog = await _auditLogService.LogAsync(
                "TestEntity",
                Guid.NewGuid(),
                "TestAction",
                null,
                new { Test = "Old Value" },
                new { Test = "New Value" },
                "127.0.0.1",
                "TestClient/1.0",
                cancellationToken);

            // Test search
            var searchParams = new AuditLogSearchParams
            {
                PageNumber = 1,
                PageSize = 5,
                EntityName = "TestEntity"
            };

            var searchResult = await _auditLogService.SearchAsync(searchParams, cancellationToken);

            // Test summary
            var summary = await _auditLogService.GetAuditSummaryAsync(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                cancellationToken);

            return this.Ok(
                $"Audit log service is working. Test log created: {testAuditLog.Id}, Search results: {searchResult.TotalCount}, Total actions in last day: {summary.TotalActions}",
                "Audit log service test completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing audit log service");
            return this.InternalServerError<string>($"Audit log service test failed: {ex.Message}");
        }
    }
}

public class LogAuditEventRequest
{
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
}