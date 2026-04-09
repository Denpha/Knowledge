namespace KMS.Domain.Entities.Interaction;

public class Comment : BaseEntity
{
    public Guid ArticleId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? ParentId { get; set; }
    
    public string Content { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true;
    
    // Navigation properties
    public virtual Knowledge.KnowledgeArticle Article { get; set; } = null!;
    public virtual Identity.AppUser Author { get; set; } = null!;
    public virtual Comment? Parent { get; set; }
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
}