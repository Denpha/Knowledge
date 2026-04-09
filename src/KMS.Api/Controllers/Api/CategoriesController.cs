using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IValidator<CreateCategoryDto> _createCategoryValidator;
    private readonly IValidator<UpdateCategoryDto> _updateCategoryValidator;

    public CategoriesController(
        ICategoryService categoryService,
        IValidator<CreateCategoryDto> createCategoryValidator,
        IValidator<UpdateCategoryDto> updateCategoryValidator)
    {
        _categoryService = categoryService;
        _createCategoryValidator = createCategoryValidator;
        _updateCategoryValidator = updateCategoryValidator;
    }

    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<CategoryDto>>> GetCategories(
        [FromQuery] string? search,
        [FromQuery] Guid? parentId,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchParams = new SearchParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search
            };

            // TODO: Add filtering for parentId and isActive once SearchParams supports it
            var result = await _categoryService.SearchAsync(searchParams, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/categories/tree
    [HttpGet("tree")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategoryTree(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _categoryService.GetTreeAsync(cancellationToken);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryService.GetByIdAsync(id, cancellationToken);

            if (category == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            return Ok(category);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/categories/{id}/children
    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<List<CategoryDto>>> GetCategoryChildren(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var children = await _categoryService.GetChildrenAsync(id, cancellationToken);
            return Ok(children);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/categories/slug/{slug}
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement GetBySlugAsync in ICategoryService
            // For now, we need to search for category with matching slug
            var searchParams = new SearchParams
            {
                PageNumber = 1,
                PageSize = 1,
                SearchTerm = slug
            };

            var result = await _categoryService.SearchAsync(searchParams, cancellationToken);
            var category = result.Items.FirstOrDefault();

            if (category == null || category.Slug != slug)
            {
                return NotFound($"Category with slug '{slug}' not found.");
            }

            return Ok(category);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/categories/{id}/has-articles
    [HttpGet("{id:guid}/has-articles")]
    public async Task<ActionResult<bool>> HasArticles(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasArticles = await _categoryService.HasArticlesAsync(id, cancellationToken);
            return Ok(hasArticles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/categories/{id}/has-subcategories
    [HttpGet("{id:guid}/has-subcategories")]
    public async Task<ActionResult<bool>> HasSubcategories(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var hasSubcategories = await _categoryService.HasSubcategoriesAsync(id, cancellationToken);
            return Ok(hasSubcategories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/categories
    [HttpPost]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        CreateCategoryDto createDto,
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
            var validationResult = await _createCategoryValidator.ValidateAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var category = await _categoryService.CreateAsync(createDto, createdById, cancellationToken);

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.Id },
                category);
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

    // PUT: api/categories/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        Guid id,
        UpdateCategoryDto updateDto,
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
            var validationResult = await _updateCategoryValidator.ValidateAsync(updateDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var category = await _categoryService.UpdateAsync(id, updateDto, updatedById, cancellationToken);

            return Ok(category);
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

    // DELETE: api/categories/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var deletedByIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(deletedByIdString) || !Guid.TryParse(deletedByIdString, out var deletedById))
            {
                return Unauthorized("Invalid user token.");
            }

            var result = await _categoryService.DeleteAsync(id, deletedById, cancellationToken);

            if (!result)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // Cannot delete category that has articles or subcategories
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}