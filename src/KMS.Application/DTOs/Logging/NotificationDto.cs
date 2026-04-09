namespace KMS.Application.DTOs.Logging;

public class NotificationDto : BaseDto
{
    public string Type { get; set; } = string.Empty; // ArticlePublished, CommentAdded, ReviewRequested, AiComplete
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReferenceUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class CreateNotificationDto : CreateDto
{
    public string Type { get; set; } = string.Empty; // ArticlePublished, CommentAdded, ReviewRequested, AiComplete
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReferenceUrl { get; set; }
    public Guid UserId { get; set; }
}

public class UpdateNotificationDto : UpdateDto
{
    public bool? IsRead { get; set; }
}

public class NotificationSearchParams : SearchParams
{
    public string? Type { get; set; }
    public Guid? UserId { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
}