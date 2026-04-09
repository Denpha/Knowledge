namespace KMS.Domain.Entities.Knowledge;

public class Tag
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int UsageCount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
}