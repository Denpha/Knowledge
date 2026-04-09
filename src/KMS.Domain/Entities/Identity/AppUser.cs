using Microsoft.AspNetCore.Identity;

namespace KMS.Domain.Entities.Identity;

public class AppUser : IdentityUser<Guid>
{
    public string FullNameTh { get; set; } = string.Empty;
    public string? FullNameEn { get; set; }
    
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? EmployeeCode { get; set; }
    
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Two-Factor Authentication
    public bool IsTwoFactorAuthEnabled { get; set; } = false;
    public string? TwoFactorSecretKey { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Knowledge.KnowledgeArticle> CreatedArticles { get; set; } = new List<Knowledge.KnowledgeArticle>();
    public virtual ICollection<Knowledge.KnowledgeArticle> UpdatedArticles { get; set; } = new List<Knowledge.KnowledgeArticle>();
    public virtual ICollection<Interaction.Comment> Comments { get; set; } = new List<Interaction.Comment>();
    public virtual ICollection<Interaction.ArticleReaction> ArticleReactions { get; set; } = new List<Interaction.ArticleReaction>();
    public virtual ICollection<Logging.AuditLog> AuditLogs { get; set; } = new List<Logging.AuditLog>();
    public virtual ICollection<Logging.Notification> Notifications { get; set; } = new List<Logging.Notification>();
}