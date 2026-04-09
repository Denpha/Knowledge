using Mapster;
using Microsoft.EntityFrameworkCore;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services;

public class TagService : ITagService
{
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<ArticleTag> _articleTagRepository;

    public TagService(
        IRepository<Tag> tagRepository,
        IRepository<ArticleTag> articleTagRepository)
    {
        _tagRepository = tagRepository;
        _articleTagRepository = articleTagRepository;
    }

    public async Task<TagDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag == null)
            return null;

        var tagDto = tag.Adapt<TagDto>();
        tagDto.ArticleCount = tag.ArticleTags?.Count ?? 0;
        return tagDto;
    }

    public async Task<PaginatedResult<TagDto>> SearchAsync(TagSearchParams searchParams, CancellationToken cancellationToken = default)
    {
        var tags = await _tagRepository.GetAllAsync(cancellationToken);

        // Apply filters
        var filteredTags = tags.AsEnumerable();

        if (!string.IsNullOrEmpty(searchParams.SearchTerm))
        {
            filteredTags = filteredTags.Where(t => 
                t.Name.Contains(searchParams.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var tagsList = filteredTags.ToList();

        // Additional filtering for ArticleId and CategoryId if provided
        // TODO: Implement these filters once we have the relationships properly set up

        var tagDtos = tagsList
            .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .Select(t => 
            {
                var dto = t.Adapt<TagDto>();
                dto.ArticleCount = t.ArticleTags?.Count ?? 0;
                return dto;
            })
            .ToList();

        return new PaginatedResult<TagDto>
        {
            Items = tagDtos,
            PageNumber = searchParams.PageNumber,
            PageSize = searchParams.PageSize,
            TotalCount = tagsList.Count
        };
    }

    public async Task<TagDto> CreateAsync(CreateTagDto createDto, Guid createdById, CancellationToken cancellationToken = default)
    {
        // Check if tag with same name already exists
        var existingTag = await _tagRepository.FindAsync(
            t => t.Name == createDto.Name, 
            cancellationToken);

        if (existingTag != null)
        {
            throw new ArgumentException($"Tag with name '{createDto.Name}' already exists.");
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Slug = GenerateSlug(createDto.Name),
            CreatedAt = DateTime.UtcNow
        };

        await _tagRepository.AddAsync(tag, cancellationToken);
        await _tagRepository.SaveChangesAsync(cancellationToken);

        var tagDto = tag.Adapt<TagDto>();
        tagDto.ArticleCount = 0;
        return tagDto;
    }

    public async Task<TagDto> UpdateAsync(Guid id, UpdateTagDto updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag == null)
        {
            throw new KeyNotFoundException($"Tag with ID {id} not found.");
        }

        // Check if new name conflicts with another tag
        if (!string.IsNullOrEmpty(updateDto.Name) && updateDto.Name != tag.Name)
        {
            var existingTag = await _tagRepository.FindAsync(
                t => t.Name == updateDto.Name && t.Id != id, 
                cancellationToken);

            if (existingTag != null)
            {
                throw new ArgumentException($"Tag with name '{updateDto.Name}' already exists.");
            }

            tag.Name = updateDto.Name;
            tag.Slug = GenerateSlug(updateDto.Name);
        }

        _tagRepository.Update(tag);
        await _tagRepository.SaveChangesAsync(cancellationToken);

        var tagDto = tag.Adapt<TagDto>();
        tagDto.ArticleCount = tag.ArticleTags?.Count ?? 0;
        return tagDto;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag == null)
        {
            return false;
        }

        // Check if tag is used by any articles
        var articleTags = await _articleTagRepository.FindAsync(
            at => at.TagId == id, 
            cancellationToken);

        if (articleTags.Any())
        {
            throw new InvalidOperationException($"Cannot delete tag '{tag.Name}' because it is used by {articleTags.Count()} article(s).");
        }

        _tagRepository.Remove(tag);
        await _tagRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<List<TagDto>> GetPopularTagsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var tags = await _tagRepository.GetAllAsync(cancellationToken);
        var popularTags = tags
            .OrderByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.ArticleTags?.Count ?? 0)
            .Take(count)
            .Select(t => 
            {
                var dto = t.Adapt<TagDto>();
                dto.ArticleCount = t.ArticleTags?.Count ?? 0;
                return dto;
            })
            .ToList();

        return popularTags;
    }

    public async Task<List<TagDto>> GetByArticleIdAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement once we have the proper relationships
        // For now, return empty list
        return new List<TagDto>();
    }

    public async Task<List<TagDto>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement once we have the proper relationships
        // For now, return empty list
        return new List<TagDto>();
    }

    private string GenerateSlug(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // Convert to lowercase and remove diacritics (Thai characters will remain as-is)
        var slug = name.ToLowerInvariant();
        
        // Replace spaces with hyphens
        slug = slug.Replace(' ', '-');
        
        // Remove invalid characters
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-ก-๙]", "");
        
        // Replace multiple hyphens with single hyphen
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
        
        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }
}