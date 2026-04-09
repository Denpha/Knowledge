using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;
    private readonly IValidator<CreateArticleDto> _createArticleValidator;
    private readonly IValidator<UpdateArticleDto> _updateArticleValidator;

    public ArticlesController(
        IArticleService articleService,
        IValidator<CreateArticleDto> createArticleValidator,
        IValidator<UpdateArticleDto> updateArticleValidator)
    {
        _articleService = articleService;
        _createArticleValidator = createArticleValidator;
        _updateArticleValidator = updateArticleValidator;
    }

    // GET: api/articles
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ArticleDto>>>> GetArticles(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? authorId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new ArticleSearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search,
                CategoryId = categoryId,
                AuthorId = authorId
            };

            var result = await _articleService.SearchAsync(searchParams, cancellationToken);
            return this.Ok(result, "Articles retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<PaginatedResult<ArticleDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ArticleDto>>> GetArticle(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var article = await _articleService.GetByIdAsync(id, cancellationToken);

            if (article == null)
            {
                return this.NotFound<ArticleDto>($"Article with ID {id} not found.");
            }

            return this.Ok(article, "Article retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<ArticleDto>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/slug/{slug}
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ApiResponse<ArticleDto>>> GetArticleBySlug(string slug, CancellationToken cancellationToken)
    {
        try
        {
            var article = await _articleService.GetBySlugAsync(slug, cancellationToken);

            if (article == null)
            {
                return this.NotFound<ArticleDto>($"Article with slug '{slug}' not found.");
            }

            return this.Ok(article, "Article retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<ArticleDto>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/articles
    [HttpPost]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<ArticleDto>> CreateArticle(
        CreateArticleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var authorIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(authorIdString) || !Guid.TryParse(authorIdString, out var authorId))
            {
                return Unauthorized("Invalid user token.");
            }

            // Convert to Application DTO
            var createDto = new CreateArticleDto
            {
                Title = request.Title,
                TitleEn = request.TitleEn,
                Content = request.Content,
                ContentEn = request.ContentEn,
                Summary = request.Summary,
                SummaryEn = request.SummaryEn,
                KeywordsEn = request.KeywordsEn,
                CategoryId = request.CategoryId,
                TagIds = request.TagIds,
                CreatedBy = authorId.ToString()
            };

            // Validate
            var validationResult = await _createArticleValidator.ValidateAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var article = await _articleService.CreateAsync(createDto, authorId, cancellationToken);

            return CreatedAtAction(
                nameof(GetArticle),
                new { id = article.Id },
                article);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/articles/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<ArticleDto>> UpdateArticle(
        Guid id,
        UpdateArticleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var updatedByIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(updatedByIdString) || !Guid.TryParse(updatedByIdString, out var updatedById))
            {
                return Unauthorized("Invalid user token.");
            }

            // Convert to Application DTO
            var updateDto = new UpdateArticleDto
            {
                Title = request.Title,
                TitleEn = request.TitleEn,
                Content = request.Content,
                ContentEn = request.ContentEn,
                Summary = request.Summary,
                SummaryEn = request.SummaryEn,
                KeywordsEn = request.KeywordsEn,
                CategoryId = request.CategoryId,
                TagIds = request.TagIds,
                UpdatedBy = updatedById.ToString()
            };

            // Validate
            var validationResult = await _updateArticleValidator.ValidateAsync(updateDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var article = await _articleService.UpdateAsync(id, updateDto, updatedById, cancellationToken);

            return Ok(article);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/articles/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteArticle(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var deletedByIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(deletedByIdString) || !Guid.TryParse(deletedByIdString, out var deletedById))
            {
                return Unauthorized("Invalid user token.");
            }

            var result = await _articleService.DeleteAsync(id, deletedById, cancellationToken);

            if (!result)
            {
                return NotFound($"Article with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PATCH: api/articles/{id}/publish
    [HttpPatch("{id:guid}/publish")]
    [Authorize(Policy = "CanPublish")]
    public async Task<ActionResult<ArticleDto>> PublishArticle(
        Guid id,
        [FromBody] PublishArticleDto? publishDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims (should be reviewer)
            var reviewerIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(reviewerIdString) || !Guid.TryParse(reviewerIdString, out var reviewerId))
            {
                return Unauthorized("Invalid user token.");
            }

            var article = await _articleService.PublishAsync(
                id,
                reviewerId,
                publishDto?.ReviewNotes,
                cancellationToken);

            return Ok(article);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/{id}/versions
    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<List<ArticleVersionDto>>> GetArticleVersions(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var versions = await _articleService.GetVersionsAsync(id, cancellationToken);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/{id}/versions/{versionNumber}
    [HttpGet("{id:guid}/versions/{versionNumber:int}")]
    public async Task<ActionResult<ArticleVersionDto>> GetArticleVersion(
        Guid id,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var version = await _articleService.GetVersionAsync(id, versionNumber, cancellationToken);
            return Ok(version);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/articles/{id}/versions/{versionNumber}/restore
    [HttpPost("{id:guid}/versions/{versionNumber:int}/restore")]
    [Authorize]
    public async Task<ActionResult<ArticleDto>> RestoreArticleVersion(
        Guid id,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var restoredByIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(restoredByIdString) || !Guid.TryParse(restoredByIdString, out var restoredById))
            {
                return Unauthorized("Invalid user token.");
            }

            var article = await _articleService.RestoreVersionAsync(id, versionNumber, restoredById, cancellationToken);
            return Ok(article);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/slug/check/{slug}
    [HttpGet("slug/check/{slug}")]
    public async Task<ActionResult<bool>> CheckSlugAvailability(
        string slug,
        [FromQuery] Guid? excludeArticleId,
        CancellationToken cancellationToken)
    {
        try
        {
            var isAvailable = await _articleService.CheckSlugAvailabilityAsync(slug, excludeArticleId, cancellationToken);
            return Ok(isAvailable);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/articles/{id}/react
    [HttpPost("{id:guid}/react")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ReactToArticle(
        Guid id,
        [FromBody] ReactToArticleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("Invalid user token.");

            var validTypes = new[] { "Like", "Bookmark", "Share" };
            if (!validTypes.Contains(request.ReactionType))
                return BadRequest("Invalid reaction type. Valid values: Like, Bookmark, Share.");

            var result = await _articleService.ToggleReactionAsync(id, userId, request.ReactionType, cancellationToken);
            return this.Ok<object>(new
            {
                result.LikeCount,
                result.BookmarkCount,
                result.ShareCount,
                result.UserLiked,
                result.UserBookmarked,
                result.UserShared
            }, "Reaction updated.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<object>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/articles/{id}/my-reaction
    [HttpGet("{id:guid}/my-reaction")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetMyReaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized("Invalid user token.");

            var result = await _articleService.GetMyReactionAsync(id, userId, cancellationToken);
            return this.Ok<object>(new
            {
                result.LikeCount,
                result.BookmarkCount,
                result.ShareCount,
                result.UserLiked,
                result.UserBookmarked,
                result.UserShared
            }, "Reaction retrieved.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<object>($"Internal server error: {ex.Message}");
        }
    }
}

// Keep these request models for backward compatibility
public class CreateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }

    public string Content { get; set; } = string.Empty;
    public string? ContentEn { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? SummaryEn { get; set; }
    public string? KeywordsEn { get; set; }

    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = new List<Guid>();
}

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

public class ReactToArticleRequest
{
    public string ReactionType { get; set; } = string.Empty; // Like, Bookmark, Share
}

public class PublishArticleDto
{
    public string? ReviewNotes { get; set; }
}