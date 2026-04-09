namespace KMS.Application.DTOs.Interaction;

public class ReactionDto : BaseDto
{
    public string Type { get; set; } = string.Empty; // Like, Bookmark, Share
    
    public Guid ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class CreateReactionDto : CreateDto
{
    public string Type { get; set; } = string.Empty; // Like, Bookmark, Share
    public Guid ArticleId { get; set; }
}

public class ArticleReactionSummaryDto
{
    public Guid ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int BookmarkCount { get; set; }
    public int ShareCount { get; set; }
    public bool? UserLiked { get; set; }
    public bool? UserBookmarked { get; set; }
    public bool? UserShared { get; set; }
}