namespace KMS.Domain.Entities.Knowledge;

public class ArticleVersion
{
    public Guid Id { get; set; }
    
    public Guid ArticleId { get; set; }
    public int VersionNumber { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string Summary { get; set; } = string.Empty;
    
    public Guid EditedById { get; set; }
    public string? ChangeNote { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual KnowledgeArticle Article { get; set; } = null!;
    public virtual Identity.AppUser EditedBy { get; set; } = null!;
}