using Microsoft.EntityFrameworkCore;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Enums;
using KMS.Domain.Interfaces;
using KMS.Infrastructure.Data;

namespace KMS.Infrastructure.Repositories;

public class ArticleRepository : Repository<KnowledgeArticle>, IArticleRepository
{
    public ArticleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<KnowledgeArticle?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.Reviewer)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);
    }

    public async Task<IEnumerable<KnowledgeArticle>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<KnowledgeArticle>> GetByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.AuthorId == authorId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<KnowledgeArticle>> GetPublishedArticlesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<KnowledgeArticle>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.Status == ArticleStatus.Published &&
                       (EF.Functions.ILike(a.Title, $"%{searchTerm}%") ||
                        EF.Functions.ILike(a.Content, $"%{searchTerm}%") ||
                        EF.Functions.ILike(a.Summary, $"%{searchTerm}%")))
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(a => a.Slug == slug, cancellationToken);
    }

    public override async Task<KnowledgeArticle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.Reviewer)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}