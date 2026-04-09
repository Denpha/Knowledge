namespace KMS.Domain.Entities.Logging;

public class Notification
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty; // ArticlePublished, CommentAdded, ReviewRequested, AiComplete
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    public string? ReferenceUrl { get; set; }
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    
    // Navigation properties
    public virtual Identity.AppUser User { get; set; } = null!;
}