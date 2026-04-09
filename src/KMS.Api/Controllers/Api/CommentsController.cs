using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Interaction;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/articles/{articleId:guid}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IValidator<CreateCommentDto> _createCommentValidator;
    private readonly IValidator<UpdateCommentDto> _updateCommentValidator;

    public CommentsController(
        ICommentService commentService,
        IValidator<CreateCommentDto> createCommentValidator,
        IValidator<UpdateCommentDto> updateCommentValidator)
    {
        _commentService = commentService;
        _createCommentValidator = createCommentValidator;
        _updateCommentValidator = updateCommentValidator;
    }

    // GET: api/articles/{articleId}/comments
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CommentDto>>>> GetComments(
        Guid articleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new CommentSearchParams
            {
                ArticleId = articleId,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            var result = await _commentService.SearchAsync(searchParams, cancellationToken);
            return this.Ok(result, "Comments retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<PaginatedResult<CommentDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/{articleId}/comments/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CommentDto>>> GetComment(
        Guid articleId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentService.GetByIdAsync(id, cancellationToken);

            if (comment == null || comment.ArticleId != articleId)
                return this.NotFound<CommentDto>($"Comment {id} not found.");

            return this.Ok(comment, "Comment retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<CommentDto>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/{articleId}/comments/{id}/replies
    [HttpGet("{id:guid}/replies")]
    public async Task<ActionResult<ApiResponse<List<CommentDto>>>> GetReplies(
        Guid articleId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var replies = await _commentService.GetRepliesAsync(id, cancellationToken);
            return this.Ok(replies, "Replies retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<List<CommentDto>>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/articles/{articleId}/comments
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CommentDto>>> CreateComment(
        Guid articleId,
        [FromBody] CreateCommentDto createDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Enforce articleId from route
            createDto.ArticleId = articleId;

            var validationResult = await _createCommentValidator.ValidateAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return this.BadRequest<CommentDto>("Validation failed.", errors);
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return this.Unauthorized<CommentDto>("User not authenticated.");

            var comment = await _commentService.CreateAsync(createDto, userId, cancellationToken);
            return this.Created<CommentDto>(
                $"/api/articles/{articleId}/comments/{comment.Id}",
                comment,
                "Comment created successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<CommentDto>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequest<CommentDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<CommentDto>($"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/articles/{articleId}/comments/{id}
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CommentDto>>> UpdateComment(
        Guid articleId,
        Guid id,
        [FromBody] UpdateCommentDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = await _updateCommentValidator.ValidateAsync(updateDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return this.BadRequest<CommentDto>("Validation failed.", errors);
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return this.Unauthorized<CommentDto>("User not authenticated.");

            var comment = await _commentService.UpdateAsync(id, updateDto, userId, cancellationToken);
            return this.Ok(comment, "Comment updated successfully.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<CommentDto>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.Forbidden<CommentDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<CommentDto>($"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/articles/{articleId}/comments/{id}
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> DeleteComment(
        Guid articleId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return this.Unauthorized("User not authenticated.");

            var existing = await _commentService.GetByIdAsync(id, cancellationToken);
            if (existing == null || existing.ArticleId != articleId)
                return this.NotFound($"Comment {id} not found.");

            // Allow author or admin to delete
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
            if (existing.AuthorId != userId && !isAdmin)
                return this.Forbidden("You do not have permission to delete this comment.");

            await _commentService.DeleteAsync(id, userId, cancellationToken);
            return this.Ok("Comment deleted successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/articles/{articleId}/comments/{id}/like
    [HttpPost("{id:guid}/like")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CommentDto>>> ToggleLike(
        Guid articleId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return this.Unauthorized<CommentDto>("User not authenticated.");

            var comment = await _commentService.ToggleLikeAsync(id, userId, cancellationToken);
            return this.Ok(comment, "Comment like toggled.");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<CommentDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.InternalServerError<CommentDto>($"Internal server error: {ex.Message}");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
