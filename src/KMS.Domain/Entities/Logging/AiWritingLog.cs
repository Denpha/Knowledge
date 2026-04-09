namespace KMS.Domain.Entities.Logging;

public class AiWritingLog
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public Guid? ArticleId { get; set; }
    
    public string FeatureType { get; set; } = string.Empty; // GenerateDraft, Improve, Summarize, Translate, AutoTag, QA
    public string? ImprovementType { get; set; } // Grammar, Concise, Formal, Expand, Simplify
    
    public string Prompt { get; set; } = string.Empty;
    public string? Response { get; set; }
    
    public string ModelUsed { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // Ollama, OpenRouter, XiaomiMiMo, AzureOpenAI, Anthropic
    
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? DurationMs { get; set; }
    
    public bool? IsAccepted { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Identity.AppUser User { get; set; } = null!;
    public virtual Knowledge.KnowledgeArticle? Article { get; set; }
}