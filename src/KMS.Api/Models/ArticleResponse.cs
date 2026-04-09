using KMS.Domain.Enums;

namespace KMS.Api.Models;

public class ArticleResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SummaryEn { get; set; }
    public string? KeywordsEn { get; set; }
    
    public ArticleStatus Status { get; set; }
    public Visibility Visibility { get; set; }
    
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    
    public Guid? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    
    public bool IsAutoTranslated { get; set; }
    public DateTime? TranslatedAt { get; set; }
    
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public List<TagResponse> Tags { get; set; } = new List<TagResponse>();
}

public class TagResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}