using KMS.Application.DTOs;
using KMS.Application.DTOs.Logging;

namespace KMS.Application.Interfaces;

public interface IAuditLogService : IBaseService<AuditLogDto, object, object, AuditLogSearchParams>
{
    // Log an audit event
    Task<AuditLogDto> LogAsync(
        string entityName,
        Guid? entityId,
        string action,
        Guid? userId,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
    
    // Log an audit event with serialized values
    Task<AuditLogDto> LogWithValuesAsync(
        string entityName,
        Guid? entityId,
        string action,
        Guid? userId,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
    
    // Get audit logs for a specific entity
    Task<PaginatedResult<AuditLogDto>> GetEntityAuditLogsAsync(
        string entityName,
        Guid entityId,
        AuditLogSearchParams searchParams,
        CancellationToken cancellationToken = default);
    
    // Get audit logs for a specific user
    Task<PaginatedResult<AuditLogDto>> GetUserAuditLogsAsync(
        Guid userId,
        AuditLogSearchParams searchParams,
        CancellationToken cancellationToken = default);
    
    // Get audit summary statistics
    Task<AuditSummaryDto> GetAuditSummaryAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
    
    // Export audit logs to CSV
    Task<byte[]> ExportToCsvAsync(
        AuditLogSearchParams searchParams,
        CancellationToken cancellationToken = default);
    
    // Clean up old audit logs
    Task<int> CleanupOldLogsAsync(int daysToKeep, CancellationToken cancellationToken = default);
}

public class AuditSummaryDto
{
    public int TotalActions { get; set; }
    public Dictionary<string, int> ActionsByType { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> ActionsByEntity { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> ActionsByUser { get; set; } = new Dictionary<string, int>();
    public Dictionary<DateTime, int> DailyActivity { get; set; } = new Dictionary<DateTime, int>();
}