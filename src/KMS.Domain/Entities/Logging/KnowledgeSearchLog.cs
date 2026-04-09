using Pgvector;

namespace KMS.Domain.Entities.Logging;

public class KnowledgeSearchLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }
    public string Query { get; set; } = string.Empty;
    public Vector? QueryEmbedding { get; set; }
    public string SearchType { get; set; } = "Keyword"; // Keyword, Semantic, AI
    public int ResultCount { get; set; } = 0;
    
    public Guid? ClickedArticleId { get; set; }
    public int? ClickedResultRank { get; set; }
    public bool HasResult { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Identity.AppUser? User { get; set; }
    public virtual Knowledge.KnowledgeArticle? ClickedArticle { get; set; }
}