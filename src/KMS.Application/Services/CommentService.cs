using Mapster;
using Microsoft.EntityFrameworkCore;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Interaction;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Interaction;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services;

public class CommentService : ICommentService
{
    private readonly IRepository<Comment> _commentRepository;

    public CommentService(IRepository<Comment> commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<CommentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.Query
            .Include(c => c.Author)
            .Include(c => c.Article)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        return comment?.Adapt<CommentDto>();
    }

    public async Task<PaginatedResult<CommentDto>> SearchAsync(CommentSearchParams searchParams, CancellationToken cancellationToken = default)
    {
        var query = _commentRepository.Query
            .Include(c => c.Author)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .Where(c => !c.IsDeleted && c.ParentId == null); // top-level only

        if (searchParams.ArticleId.HasValue)
            query = query.Where(c => c.ArticleId == searchParams.ArticleId.Value);

        if (searchParams.AuthorId.HasValue)
            query = query.Where(c => c.AuthorId == searchParams.AuthorId.Value);

        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
            query = query.Where(c => c.Content.Contains(searchParams.SearchTerm));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CommentDto>
        {
            Items = items.Select(c => c.Adapt<CommentDto>()).ToList(),
            PageNumber = searchParams.PageNumber,
            PageSize = searchParams.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<CommentDto> CreateAsync(CreateCommentDto createDto, Guid createdById, CancellationToken cancellationToken = default)
    {
        // Verify parent exists if provided
        if (createDto.ParentId.HasValue)
        {
            var parent = await _commentRepository.GetByIdAsync(createDto.ParentId.Value, cancellationToken);
            if (parent == null || parent.IsDeleted)
                throw new KeyNotFoundException($"Parent comment {createDto.ParentId.Value} not found.");

            // Only allow one level of nesting
            if (parent.ParentId.HasValue)
                throw new InvalidOperationException("Nested replies beyond one level are not allowed.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ArticleId = createDto.ArticleId,
            AuthorId = createdById,
            ParentId = createDto.ParentId,
            Content = createDto.Content,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdById.ToString(),
        };

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _commentRepository.SaveChangesAsync(cancellationToken);

        var created = await GetByIdAsync(comment.Id, cancellationToken);
        return created!;
    }

    public async Task<CommentDto> UpdateAsync(Guid id, UpdateCommentDto updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
        if (comment == null || comment.IsDeleted)
            throw new KeyNotFoundException($"Comment {id} not found.");

        if (comment.AuthorId != updatedById)
            throw new UnauthorizedAccessException("You can only edit your own comments.");

        if (!string.IsNullOrEmpty(updateDto.Content))
            comment.Content = updateDto.Content;

        comment.UpdatedAt = DateTime.UtcNow;
        comment.UpdatedBy = updatedById.ToString();

        _commentRepository.Update(comment);
        await _commentRepository.SaveChangesAsync(cancellationToken);

        var updated = await GetByIdAsync(id, cancellationToken);
        return updated!;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
        if (comment == null || comment.IsDeleted)
            return false;

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedBy = deletedById.ToString();

        _commentRepository.Update(comment);
        await _commentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<CommentDto>> GetRepliesAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var replies = await _commentRepository.Query
            .Include(c => c.Author)
            .Where(c => c.ParentId == commentId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return replies.Select(c => c.Adapt<CommentDto>()).ToList();
    }

    public async Task<int> GetCommentCountAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        return await _commentRepository.Query
            .CountAsync(c => c.ArticleId == articleId && !c.IsDeleted, cancellationToken);
    }

    public async Task<CommentDto> ToggleLikeAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Like toggling requires a separate likes table; this is a placeholder
        // that just returns the current comment unchanged.
        var comment = await GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
            throw new KeyNotFoundException($"Comment {commentId} not found.");
        return comment;
    }

    public async Task<bool> DeleteReplyAsync(Guid commentId, Guid replyId, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var reply = await _commentRepository.GetByIdAsync(replyId, cancellationToken);
        if (reply == null || reply.ParentId != commentId || reply.IsDeleted)
            return false;

        return await DeleteAsync(replyId, deletedById, cancellationToken);
    }
}
