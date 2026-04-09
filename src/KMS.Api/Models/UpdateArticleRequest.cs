namespace KMS.Api.Models;

public class UpdateArticleRequest
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