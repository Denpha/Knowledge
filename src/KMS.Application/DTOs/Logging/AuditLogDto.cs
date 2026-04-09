namespace KMS.Application.DTOs.Logging;

public class AuditLogDto : BaseDto
{
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
}

public class AuditLogSearchParams : SearchParams
{
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? Action { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}