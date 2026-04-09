namespace KMS.Domain.Entities;

public class SystemSetting
{
    public Guid Id { get; set; }
    
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    
    public bool IsEncrypted { get; set; } = false;
    
    public Guid? UpdatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Identity.AppUser? UpdatedBy { get; set; }
}