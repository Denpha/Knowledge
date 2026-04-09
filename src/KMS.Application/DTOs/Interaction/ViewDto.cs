namespace KMS.Application.DTOs.Interaction;

public class ViewDto : BaseDto
{
    public Guid ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ViewedAt { get; set; }
}

public class ViewSearchParams : SearchParams
{
    public Guid? ArticleId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? ViewedFrom { get; set; }
    public DateTime? ViewedTo { get; set; }
}