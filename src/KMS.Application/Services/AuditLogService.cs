using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Logging;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Logging;
using KMS.Domain.Interfaces;
using System.Text.Json;

namespace KMS.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IRepository<AuditLog> _auditLogRepository;
    private readonly IRepository<Domain.Entities.Identity.AppUser> _userRepository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IRepository<AuditLog> auditLogRepository,
        IRepository<Domain.Entities.Identity.AppUser> userRepository,
        ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var auditLog = await _auditLogRepository.GetByIdAsync(id, cancellationToken);
        if (auditLog == null) return null;

        // Load user if needed (lazy loading might be enabled)
        return auditLog.Adapt<AuditLogDto>();
    }

    // Note: CreateAsync is not implemented for audit logs since they should only be created via LogAsync
    public Task<AuditLogDto> CreateAsync(object createDto, Guid createdById, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use LogAsync or LogWithValuesAsync to create audit logs.");
    }

    // Note: UpdateAsync is not implemented for audit logs since they are immutable
    public Task<AuditLogDto> UpdateAsync(Guid id, object updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Audit logs are immutable and cannot be updated.");
    }

    // Note: DeleteAsync is not implemented for audit logs since they should be preserved
    public Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Audit logs are append-only and cannot be deleted.");
    }

    public async Task<PaginatedResult<AuditLogDto>> SearchAsync(AuditLogSearchParams searchParams, CancellationToken cancellationToken = default)
    {
        // Get all audit logs first (in a real application with large datasets, this should be optimized)
        var allAuditLogs = await _auditLogRepository.GetAllAsync(cancellationToken);
        
        // Apply filters in memory
        var filteredAuditLogs = allAuditLogs.AsEnumerable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchParams.EntityName))
        {
            filteredAuditLogs = filteredAuditLogs.Where(a => a.EntityName == searchParams.EntityName);
        }

        if (searchParams.EntityId.HasValue)
        {
            filteredAuditLogs = filteredAuditLogs.Where(a => a.EntityId == searchParams.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Action))
        {
            filteredAuditLogs = filteredAuditLogs.Where(a => a.Action == searchParams.Action);
        }

        if (searchParams.UserId.HasValue)
        {
            filteredAuditLogs = filteredAuditLogs.Where(a => a.UserId == searchParams.UserId.Value);
        }

        if (searchParams.FromDate.HasValue)
        {
            filteredAuditLogs = filteredAuditLogs.Where(a => a.CreatedAt >= searchParams.FromDate.Value);
        }

        if (searchParams.ToDate.HasValue)
        {
            filteredAuditLogs = filteredAuditLogs.Where(a => a.CreatedAt <= searchParams.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.SearchTerm))
        {
            filteredAuditLogs = filteredAuditLogs.Where(a => 
                a.EntityName.Contains(searchParams.SearchTerm) || 
                a.Action.Contains(searchParams.SearchTerm) ||
                (a.OldValues != null && a.OldValues.Contains(searchParams.SearchTerm)) ||
                (a.NewValues != null && a.NewValues.Contains(searchParams.SearchTerm)));
        }

        // Apply sorting (default: newest first)
        filteredAuditLogs = searchParams.SortDescending
            ? filteredAuditLogs.OrderByDescending(GetSortProperty(searchParams))
            : filteredAuditLogs.OrderBy(GetSortProperty(searchParams));

        // Convert to list for pagination
        var filteredList = filteredAuditLogs.ToList();
        
        // Get total count
        var totalCount = filteredList.Count;

        // Apply pagination
        var items = filteredList
            .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToList();

        var dtos = items.Select(a =>
        {
            var dto = a.Adapt<AuditLogDto>();
            dto.UserName = a.User?.FullNameTh ?? a.User?.UserName ?? string.Empty;
            return dto;
        }).ToList();

        return new PaginatedResult<AuditLogDto>
        {
            Items = dtos,
            PageNumber = searchParams.PageNumber,
            PageSize = searchParams.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AuditLogDto> LogAsync(
        string entityName,
        Guid? entityId,
        string action,
        Guid? userId,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var oldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null;
        var newValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null;

        return await LogWithValuesAsync(
            entityName,
            entityId,
            action,
            userId,
            oldValuesJson,
            newValuesJson,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<AuditLogDto> LogWithValuesAsync(
        string entityName,
        Guid? entityId,
        string action,
        Guid? userId,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException("Entity name is required.", nameof(entityName));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        var auditLog = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            OldValues = oldValuesJson,
            NewValues = newValuesJson,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit log created: {EntityName} {EntityId} {Action} by {UserId}",
            entityName, entityId, action, userId);

        return auditLog.Adapt<AuditLogDto>();
    }

    public async Task<PaginatedResult<AuditLogDto>> GetEntityAuditLogsAsync(
        string entityName,
        Guid entityId,
        AuditLogSearchParams searchParams,
        CancellationToken cancellationToken = default)
    {
        searchParams.EntityName = entityName;
        searchParams.EntityId = entityId;
        return await SearchAsync(searchParams, cancellationToken);
    }

    public async Task<PaginatedResult<AuditLogDto>> GetUserAuditLogsAsync(
        Guid userId,
        AuditLogSearchParams searchParams,
        CancellationToken cancellationToken = default)
    {
        searchParams.UserId = userId;
        return await SearchAsync(searchParams, cancellationToken);
    }

    public async Task<AuditSummaryDto> GetAuditSummaryAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var summary = new AuditSummaryDto();

        // Get all logs in date range
        var logs = await _auditLogRepository.Query
            .Include(a => a.User)
            .Where(a => a.CreatedAt >= fromDate && a.CreatedAt <= toDate)
            .ToListAsync(cancellationToken);

        summary.TotalActions = logs.Count;

        // Group by action type
        summary.ActionsByType = logs
            .GroupBy(a => a.Action)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by entity name
        summary.ActionsByEntity = logs
            .GroupBy(a => a.EntityName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by user
        summary.ActionsByUser = logs
            .Where(a => a.UserId.HasValue)
            .GroupBy(a => a.User?.UserName ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        // Group by day
        summary.DailyActivity = logs
            .GroupBy(a => a.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        return summary;
    }

    public async Task<byte[]> ExportToCsvAsync(
        AuditLogSearchParams searchParams,
        CancellationToken cancellationToken = default)
    {
        // Get all logs matching the search params (without pagination)
        var query = _auditLogRepository.Query
            .Include(a => a.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchParams.EntityName))
        {
            query = query.Where(a => a.EntityName == searchParams.EntityName);
        }

        if (searchParams.EntityId.HasValue)
        {
            query = query.Where(a => a.EntityId == searchParams.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Action))
        {
            query = query.Where(a => a.Action == searchParams.Action);
        }

        if (searchParams.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == searchParams.UserId.Value);
        }

        if (searchParams.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= searchParams.FromDate.Value);
        }

        if (searchParams.ToDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt <= searchParams.ToDate.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        // Generate CSV
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);

        // Write header
        writer.WriteLine("Id,CreatedAt,EntityName,EntityId,Action,UserId,UserName,IpAddress,UserAgent,OldValues,NewValues");

        // Write rows
        foreach (var log in logs)
        {
            var userName = log.User?.UserName ?? "System";
            var oldValues = EscapeCsvField(log.OldValues);
            var newValues = EscapeCsvField(log.NewValues);
            var ipAddress = EscapeCsvField(log.IpAddress);
            var userAgent = EscapeCsvField(log.UserAgent);

            writer.WriteLine($"{log.Id},{log.CreatedAt:yyyy-MM-dd HH:mm:ss},{EscapeCsvField(log.EntityName)},{log.EntityId},{EscapeCsvField(log.Action)},{log.UserId},{EscapeCsvField(userName)},{ipAddress},{userAgent},{oldValues},{newValues}");
        }

        writer.Flush();
        return memoryStream.ToArray();
    }

    public async Task<int> CleanupOldLogsAsync(int daysToKeep, CancellationToken cancellationToken = default)
    {
        if (daysToKeep < 1)
        {
            throw new ArgumentException("Days to keep must be at least 1.", nameof(daysToKeep));
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var oldLogs = await _auditLogRepository.Query
            .Where(a => a.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (!oldLogs.Any())
        {
            return 0;
        }

        foreach (var log in oldLogs)
        {
            _auditLogRepository.Remove(log);
        }

        await _auditLogRepository.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Cleaned up {Count} audit logs older than {CutoffDate}", oldLogs.Count, cutoffDate);
        return oldLogs.Count;
    }

    #region Private Methods

    private static Func<AuditLog, object> GetSortProperty(AuditLogSearchParams searchParams)
    {
        return searchParams.SortBy?.ToLower() switch
        {
            "entityname" => a => a.EntityName,
            "action" => a => a.Action,
            "userid" => a => (object)(a.UserId ?? Guid.Empty),
            "createdat" => a => a.CreatedAt,
            _ => a => a.CreatedAt
        };
    }

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // If the field contains commas, quotes, or newlines, wrap it in quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape quotes by doubling them
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
    }

    #endregion
}