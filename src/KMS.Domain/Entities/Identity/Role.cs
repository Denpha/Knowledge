using Microsoft.AspNetCore.Identity;

namespace KMS.Domain.Entities.Identity;

public class Role : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public string? Permissions { get; set; } // JSON array of permissions
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}