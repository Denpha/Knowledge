namespace KMS.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    
    public string ClientType { get; set; } = string.Empty; // OpenClaw, LineWebhook, ThirdParty
    public string Permissions { get; set; } = string.Empty; // JSON array
    
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    
    public DateTime? LastUsedAt { get; set; }
    public int UsageCount { get; set; } = 0;
    
    public string? Description { get; set; }
    public string? AllowedIps { get; set; } // JSON array
    
    public Guid? CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedById { get; set; }
    
    // Navigation properties
    public virtual Identity.AppUser? CreatedBy { get; set; }
    public virtual Identity.AppUser? RevokedBy { get; set; }
}