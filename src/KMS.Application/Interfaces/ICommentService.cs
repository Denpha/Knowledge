using KMS.Application.DTOs.Interaction;

namespace KMS.Application.Interfaces;

public interface ICommentService : IBaseService<CommentDto, CreateCommentDto, UpdateCommentDto, CommentSearchParams>
{
    Task<List<CommentDto>> GetRepliesAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<int> GetCommentCountAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<CommentDto> ToggleLikeAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteReplyAsync(Guid commentId, Guid replyId, Guid deletedById, CancellationToken cancellationToken = default);
}