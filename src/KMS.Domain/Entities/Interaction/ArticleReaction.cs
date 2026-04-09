namespace KMS.Domain.Entities.Interaction;

public class ArticleReaction
{
    public Guid Id { get; set; }
    
    public Guid ArticleId { get; set; }
    public Guid UserId { get; set; }
    
    public string ReactionType { get; set; } = string.Empty; // Like, Bookmark, Share
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Knowledge.KnowledgeArticle Article { get; set; } = null!;
    public virtual Identity.AppUser User { get; set; } = null!;
}