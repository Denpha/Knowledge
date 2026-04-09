using KMS.Domain.Enums;

namespace KMS.Application.DTOs.Knowledge;

public class ArticleDto : BaseDto
{
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
    public string? CategoryPath { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;

    public Guid? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }

    public bool IsAutoTranslated { get; set; }
    public DateTime? TranslatedAt { get; set; }

    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int BookmarkCount { get; set; }
    public int CommentCount { get; set; }

    public DateTime? PublishedAt { get; set; }

    public List<TagDto> Tags { get; set; } = new List<TagDto>();
    public List<ArticleVersionDto>? Versions { get; set; }
}

public class CreateArticleDto : CreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }

    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SummaryEn { get; set; }
    public string? KeywordsEn { get; set; }

    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = new List<Guid>();
}

public class UpdateArticleDto : UpdateDto
{
    public string? Title { get; set; }
    public string? TitleEn { get; set; }

    public string? Content { get; set; }
    public string? ContentEn { get; set; }
    public string? Summary { get; set; }
    public string? SummaryEn { get; set; }
    public string? KeywordsEn { get; set; }

    public Guid? CategoryId { get; set; }
    public List<Guid>? TagIds { get; set; }
}

public class PublishArticleDto
{
    public string? ReviewNotes { get; set; }
}

public class ArticleSearchParams : SearchParams
{
    public Guid? CategoryId { get; set; }
    public Guid? AuthorId { get; set; }
    public Guid? TagId { get; set; }
    public ArticleStatus? Status { get; set; }
    public Visibility? Visibility { get; set; }
    public DateTime? PublishedFrom { get; set; }
    public DateTime? PublishedTo { get; set; }
    public bool IncludeDrafts { get; set; } = false;
    public bool IncludeUnpublished { get; set; } = false;
}