namespace KMS.Domain.Entities.Knowledge;

public class ArticleTag
{
    public Guid ArticleId { get; set; }
    public Guid TagId { get; set; }
    
    // Navigation properties
    public virtual KnowledgeArticle Article { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}