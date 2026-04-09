namespace KMS.Application.DTOs.Interaction;

public class CommentDto : BaseDto
{
    public string Content { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    
    public Guid ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    
    public Guid? ParentId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    
    public int LikeCount { get; set; }
    public bool IsEdited { get; set; }
    
    public List<CommentDto>? Replies { get; set; }
}

public class CreateCommentDto : CreateDto
{
    public string Content { get; set; } = string.Empty;
    public Guid ArticleId { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsAnonymous { get; set; }
}

public class UpdateCommentDto : UpdateDto
{
    public string? Content { get; set; }
    public bool? IsAnonymous { get; set; }
}

public class CommentSearchParams : SearchParams
{
    public Guid? ArticleId { get; set; }
    public Guid? AuthorId { get; set; }
    public Guid? ParentId { get; set; }
    public bool? IsAnonymous { get; set; }
}