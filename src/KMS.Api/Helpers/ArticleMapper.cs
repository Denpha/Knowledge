using KMS.Api.Models;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Enums;

namespace KMS.Api.Helpers;

public static class ArticleMapper
{
    public static ArticleResponse ToResponse(KnowledgeArticle article)
    {
        return new ArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            TitleEn = article.TitleEn,
            Slug = article.Slug,
            Content = article.Content,
            ContentEn = article.ContentEn,
            Summary = article.Summary,
            SummaryEn = article.SummaryEn,
            KeywordsEn = article.KeywordsEn,
            Status = article.Status,
            Visibility = article.Visibility,
            CategoryId = article.CategoryId,
            CategoryName = article.Category?.Name ?? string.Empty,
            AuthorId = article.AuthorId,
            AuthorName = article.Author?.FullNameTh ?? string.Empty,
            ReviewerId = article.ReviewerId,
            ReviewerName = article.Reviewer?.FullNameTh,
            IsAutoTranslated = article.IsAutoTranslated,
            TranslatedAt = article.TranslatedAt,
            ViewCount = article.ViewCount,
            LikeCount = article.LikeCount,
            PublishedAt = article.PublishedAt,
            CreatedAt = article.CreatedAt,
            UpdatedAt = article.UpdatedAt,
            Tags = article.ArticleTags.Select(at => new TagResponse
            {
                Id = at.Tag.Id,
                Name = at.Tag.Name,
                Slug = at.Tag.Slug
            }).ToList()
        };
    }

    public static KnowledgeArticle ToEntity(CreateArticleRequest request, Guid authorId)
    {
        // Generate slug from title
        var slug = GenerateSlug(request.Title);
        
        return new KnowledgeArticle
        {
            Title = request.Title,
            TitleEn = request.TitleEn,
            Slug = slug,
            Content = request.Content,
            ContentEn = request.ContentEn,
            Summary = request.Summary,
            SummaryEn = request.SummaryEn,
            KeywordsEn = request.KeywordsEn,
            Status = ArticleStatus.Draft,
            Visibility = Visibility.Internal,
            CategoryId = request.CategoryId,
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(KnowledgeArticle article, UpdateArticleRequest request)
    {
        if (!string.IsNullOrEmpty(request.Title))
        {
            article.Title = request.Title;
            article.Slug = GenerateSlug(request.Title);
        }
        
        if (request.TitleEn != null) article.TitleEn = request.TitleEn;
        if (request.Content != null) article.Content = request.Content;
        if (request.ContentEn != null) article.ContentEn = request.ContentEn;
        if (request.Summary != null) article.Summary = request.Summary;
        if (request.SummaryEn != null) article.SummaryEn = request.SummaryEn;
        if (request.KeywordsEn != null) article.KeywordsEn = request.KeywordsEn;
        if (request.CategoryId.HasValue) article.CategoryId = request.CategoryId.Value;
        
        article.UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateSlug(string title)
    {
        // Simple slug generation - in production, use a proper slug library
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Replace("---", "-")
            .Replace(":", "")
            .Replace(";", "")
            .Replace(",", "")
            .Replace(".", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("`", "");
        
        // Limit length
        if (slug.Length > 100)
        {
            slug = slug.Substring(0, 100);
        }
        
        return slug;
    }
}