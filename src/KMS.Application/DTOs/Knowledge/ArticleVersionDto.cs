namespace KMS.Application.DTOs.Knowledge;

public class ArticleVersionDto : BaseDto
{
    public Guid ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    public int VersionNumber { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string Summary { get; set; } = string.Empty;

    public Guid EditedById { get; set; }
    public string EditedByName { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
}

public class CreateArticleVersionDto : CreateDto
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
}