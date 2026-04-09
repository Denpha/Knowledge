namespace KMS.Domain.Entities.Logging;

public class AuditLog
{
    public Guid Id { get; set; }
    
    public Guid? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Publish, DirectPublish, SubmitForReview, Login
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Identity.AppUser? User { get; set; }
}