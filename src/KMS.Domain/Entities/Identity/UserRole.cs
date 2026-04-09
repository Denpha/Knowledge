namespace KMS.Domain.Entities.Identity;

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    
    // Navigation properties
    public virtual AppUser User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}