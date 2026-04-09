namespace KMS.Domain.Entities.Knowledge;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public Guid? ParentId { get; set; }
    public string? IconName { get; set; }
    public string? ColorHex { get; set; }
    
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Category? Parent { get; set; }
    public virtual ICollection<Category> Children { get; set; } = new List<Category>();
    public virtual ICollection<KnowledgeArticle> Articles { get; set; } = new List<KnowledgeArticle>();
}