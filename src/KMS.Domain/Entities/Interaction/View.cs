namespace KMS.Domain.Entities.Interaction;

public class View
{
    public Guid Id { get; set; }
    
    public Guid ArticleId { get; set; }
    public Guid? UserId { get; set; }
    
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    
    public DateTime ViewedAt { get; set; }
    
    // Navigation properties
    public virtual Knowledge.KnowledgeArticle Article { get; set; } = null!;
    public virtual Identity.AppUser? User { get; set; }
}