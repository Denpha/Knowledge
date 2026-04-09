using Mapster;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _categoryRepository;

    public CategoryService(IRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return category?.Adapt<CategoryDto>();
    }

    public async Task<PaginatedResult<CategoryDto>> SearchAsync(SearchParams searchParams, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        
        // Simple search implementation
        var filteredCategories = categories
            .Where(c => string.IsNullOrEmpty(searchParams.SearchTerm) ||
                       c.Name.Contains(searchParams.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                       (c.Description != null && c.Description.Contains(searchParams.SearchTerm, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var categoryDtos = filteredCategories
            .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .Select(c => c.Adapt<CategoryDto>())
            .ToList();

        return new PaginatedResult<CategoryDto>
        {
            Items = categoryDtos,
            PageNumber = searchParams.PageNumber,
            PageSize = searchParams.PageSize,
            TotalCount = filteredCategories.Count
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto createDto, Guid createdById, CancellationToken cancellationToken = default)
    {
        // Check if parent exists
        if (createDto.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(createDto.ParentId.Value, cancellationToken);
            if (parent == null)
            {
                throw new ArgumentException($"Parent category with ID {createDto.ParentId.Value} not found.");
            }
        }

        // Generate slug from name
        var slug = GenerateSlug(createDto.Name);

        var category = new Category
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Slug = slug,
            ParentId = createDto.ParentId,
            SortOrder = createDto.Order,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        // Reload with includes
        var createdCategory = await _categoryRepository.GetByIdAsync(category.Id, cancellationToken);
        return createdCategory!.Adapt<CategoryDto>();
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Category with ID {id} not found.");
        }

        // Check if trying to set itself as parent
        if (updateDto.ParentId.HasValue && updateDto.ParentId.Value == id)
        {
            throw new ArgumentException("Category cannot be its own parent.");
        }

        // Check if parent exists
        if (updateDto.ParentId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(updateDto.ParentId.Value, cancellationToken);
            if (parent == null)
            {
                throw new ArgumentException($"Parent category with ID {updateDto.ParentId.Value} not found.");
            }
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(updateDto.Name))
        {
            category.Name = updateDto.Name;
            
            // Update slug if name changed
            var newSlug = GenerateSlug(updateDto.Name);
            if (newSlug != category.Slug)
            {
                // Check if slug already exists
                var categories = await _categoryRepository.GetAllAsync(cancellationToken);
                var slugExists = categories.Any(c => c.Slug == newSlug && c.Id != id);
                
                category.Slug = slugExists ? $"{newSlug}-{DateTime.UtcNow:yyyyMMddHHmmss}" : newSlug;
            }
        }

        if (updateDto.Description != null) category.Description = updateDto.Description;
        if (updateDto.ParentId != null) category.ParentId = updateDto.ParentId;
        if (updateDto.Order.HasValue) category.SortOrder = updateDto.Order.Value;
        if (updateDto.IsActive.HasValue) category.IsActive = updateDto.IsActive.Value;
        
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        // Reload with includes
        var updatedCategory = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        return updatedCategory!.Adapt<CategoryDto>();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return false;
        }

        // Check if category has articles
        if (category.Articles != null && category.Articles.Any())
        {
            throw new InvalidOperationException("Cannot delete category that has articles. Please move or delete articles first.");
        }

        // Check if category has subcategories
        if (category.Children != null && category.Children.Any())
        {
            throw new InvalidOperationException("Cannot delete category that has subcategories. Please delete or move subcategories first.");
        }

        _categoryRepository.Remove(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<List<CategoryDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);

        // Get root categories (no parent)
        var rootCategories = categories
            .Where(c => c.ParentId == null && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();

        var result = new List<CategoryDto>();
        
        foreach (var rootCategory in rootCategories)
        {
            var rootDto = rootCategory.Adapt<CategoryDto>();
            BuildCategoryTree(rootDto, categories);
            result.Add(rootDto);
        }

        return result;
    }

    public async Task<List<CategoryDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var children = categories
            .Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();

        return children.Adapt<List<CategoryDto>>();
    }

    public async Task<bool> HasArticlesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        return category?.Articles != null && category.Articles.Any();
    }

    public async Task<bool> HasSubcategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        return category?.Children != null && category.Children.Any();
    }

    private static void BuildCategoryTree(CategoryDto categoryDto, IEnumerable<Category> allCategories)
    {
        var childCategories = allCategories
            .Where(c => c.ParentId == categoryDto.Id && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();

        if (childCategories.Any())
        {
            categoryDto.SubCategories = new List<CategoryDto>();
            
            foreach (var childCategory in childCategories)
            {
                var childDto = childCategory.Adapt<CategoryDto>();
                BuildCategoryTree(childDto, allCategories);
                categoryDto.SubCategories.Add(childDto);
            }
        }
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace(".", "-")
            .Replace(",", "-")
            .Replace(";", "-")
            .Replace(":", "-")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("`", "")
            .Replace("~", "")
            .Replace("@", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("^", "")
            .Replace("&", "")
            .Replace("*", "")
            .Replace("+", "")
            .Replace("=", "")
            .Replace("|", "")
            .Replace("\\", "")
            .Replace("/", "-")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("--", "-")
            .Replace("--", "-")
            .Trim('-');
    }
}