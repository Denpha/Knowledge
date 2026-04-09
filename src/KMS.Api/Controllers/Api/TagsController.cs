using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IValidator<CreateTagDto> _createTagValidator;
    private readonly IValidator<UpdateTagDto> _updateTagValidator;

    public TagsController(
        ITagService tagService,
        IValidator<CreateTagDto> createTagValidator,
        IValidator<UpdateTagDto> updateTagValidator)
    {
        _tagService = tagService;
        _createTagValidator = createTagValidator;
        _updateTagValidator = updateTagValidator;
    }

    // GET: api/tags
    [HttpGet]
    public async Task<ActionResult<dynamic>> GetTags(
        [FromQuery] string? search,
        [FromQuery] Guid? articleId,
        [FromQuery] Guid? categoryId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new TagSearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search,
                ArticleId = articleId,
                CategoryId = categoryId
            };

            var result = await _tagService.SearchAsync(searchParams, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/tags/popular
    [HttpGet("popular")]
    public async Task<ActionResult<List<TagDto>>> GetPopularTags(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagService.GetPopularTagsAsync(count, cancellationToken);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/tags/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> GetTag(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagService.GetByIdAsync(id, cancellationToken);

            if (tag == null)
            {
                return NotFound($"Tag with ID {id} not found.");
            }

            return Ok(tag);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/tags/slug/{slug}
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<TagDto>> GetTagBySlug(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            // Search for tag with matching slug
            var searchParams = new TagSearchParams
            {
                PageNumber = 1,
                PageSize = 1,
                SearchTerm = slug
            };

            var result = await _tagService.SearchAsync(searchParams, cancellationToken);
            var tag = result.Items.FirstOrDefault(t => t.Slug == slug);

            if (tag == null)
            {
                return NotFound($"Tag with slug '{slug}' not found.");
            }

            return Ok(tag);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/tags/article/{articleId}
    [HttpGet("article/{articleId:guid}")]
    public async Task<ActionResult<List<TagDto>>> GetTagsByArticle(Guid articleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagService.GetByArticleIdAsync(articleId, cancellationToken);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/tags/category/{categoryId}
    [HttpGet("category/{categoryId:guid}")]
    public async Task<ActionResult<List<TagDto>>> GetTagsByCategory(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagService.GetByCategoryIdAsync(categoryId, cancellationToken);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/tags
    [HttpPost]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<TagDto>> CreateTag(
        CreateTagDto createDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var createdByIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(createdByIdString) || !Guid.TryParse(createdByIdString, out var createdById))
            {
                return Unauthorized("Invalid user token.");
            }
            createDto.CreatedBy = createdById.ToString();

            // Validate
            var validationResult = await _createTagValidator.ValidateAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var tag = await _tagService.CreateAsync(createDto, createdById, cancellationToken);

            return CreatedAtAction(
                nameof(GetTag),
                new { id = tag.Id },
                tag);
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

    // PUT: api/tags/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<TagDto>> UpdateTag(
        Guid id,
        UpdateTagDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var updatedByIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(updatedByIdString) || !Guid.TryParse(updatedByIdString, out var updatedById))
            {
                return Unauthorized("Invalid user token.");
            }
            updateDto.UpdatedBy = updatedById.ToString();

            // Validate
            var validationResult = await _updateTagValidator.ValidateAsync(updateDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var tag = await _tagService.UpdateAsync(id, updateDto, updatedById, cancellationToken);

            return Ok(tag);
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

    // DELETE: api/tags/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var deletedByIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(deletedByIdString) || !Guid.TryParse(deletedByIdString, out var deletedById))
            {
                return Unauthorized("Invalid user token.");
            }

            var result = await _tagService.DeleteAsync(id, deletedById, cancellationToken);

            if (!result)
            {
                return NotFound($"Tag with ID {id} not found.");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // Cannot delete tag that is used by articles
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}