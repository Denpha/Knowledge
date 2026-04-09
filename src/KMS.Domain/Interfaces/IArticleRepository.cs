using KMS.Domain.Entities.Knowledge;

namespace KMS.Domain.Interfaces;

public interface IArticleRepository : IRepository<KnowledgeArticle>
{
    Task<KnowledgeArticle?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeArticle>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeArticle>> GetByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeArticle>> GetPublishedArticlesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeArticle>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
}