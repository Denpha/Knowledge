using Pgvector;
using KMS.Domain.Enums;

namespace KMS.Domain.Entities.Knowledge;

public class KnowledgeArticle : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SummaryEn { get; set; }
    public string? KeywordsEn { get; set; }
    
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public Visibility Visibility { get; set; } = Visibility.Internal;
    
    public Guid CategoryId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? ReviewerId { get; set; }

    public bool IsAutoTranslated { get; set; } = false;
    public DateTime? TranslatedAt { get; set; }

    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;

    public Vector? Embedding { get; set; } // pgvector embedding

    public DateTime? PublishedAt { get; set; }
    public DateTime? ReviewRequestedAt { get; set; }
    
    // Navigation properties
    public virtual Category Category { get; set; } = null!;
    public virtual Identity.AppUser Author { get; set; } = null!;
    public virtual Identity.AppUser? Reviewer { get; set; }
    
    public virtual ICollection<ArticleVersion> Versions { get; set; } = new List<ArticleVersion>();
    public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    public virtual ICollection<Interaction.Comment> Comments { get; set; } = new List<Interaction.Comment>();
    public virtual ICollection<Interaction.ArticleReaction> ArticleReactions { get; set; } = new List<Interaction.ArticleReaction>();
    public virtual ICollection<Interaction.View> Views { get; set; } = new List<Interaction.View>();
}