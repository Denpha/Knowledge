namespace KMS.Application.DTOs.Knowledge;

public class TagDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int UsageCount { get; set; } = 0;
    public int ArticleCount { get; set; }
}

public class CreateTagDto : CreateDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateTagDto : UpdateDto
{
    public string? Name { get; set; }
}

public class TagSearchParams : SearchParams
{
    public Guid? ArticleId { get; set; }
    public Guid? CategoryId { get; set; }
}