namespace KMS.Api.Models;

public class CreateArticleRequest
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