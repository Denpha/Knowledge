using KMS.Application.DTOs;
using KMS.Application.DTOs.Interaction;
using KMS.Application.DTOs.Knowledge;

namespace KMS.Application.Interfaces;

public interface IArticleService
{
    Task<ArticleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ArticleDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<PaginatedResult<ArticleDto>> SearchAsync(ArticleSearchParams searchParams, CancellationToken cancellationToken = default);
    Task<ArticleDto> CreateAsync(CreateArticleDto createDto, Guid authorId, CancellationToken cancellationToken = default);
    Task<ArticleDto> UpdateAsync(Guid id, UpdateArticleDto updateDto, Guid updatedById, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
    Task<ArticleDto> PublishAsync(Guid id, Guid publisherId, string? reviewNotes = null, CancellationToken cancellationToken = default);
    Task<ArticleDto> ArchiveAsync(Guid id, Guid archivedById, CancellationToken cancellationToken = default);
    Task<bool> CheckSlugAvailabilityAsync(string slug, Guid? excludeArticleId = null, CancellationToken cancellationToken = default);
    Task<List<ArticleVersionDto>> GetVersionsAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<ArticleVersionDto> GetVersionAsync(Guid articleId, int versionNumber, CancellationToken cancellationToken = default);
    Task<ArticleDto> RestoreVersionAsync(Guid articleId, int versionNumber, Guid restoredById, CancellationToken cancellationToken = default);
    
    // Publish-First workflow methods
    Task<ArticleDto> ReviewApproveAsync(Guid articleId, Guid reviewerId, string? reviewNotes = null, CancellationToken cancellationToken = default);
    Task<ArticleDto> ReviewRejectAsync(Guid articleId, Guid reviewerId, string rejectionReason, CancellationToken cancellationToken = default);
    Task<List<ArticleDto>> GetArticlesRequiringReviewAsync(Guid reviewerId, CancellationToken cancellationToken = default);
    Task<List<ArticleDto>> GetMyArticlesUnderReviewAsync(Guid authorId, CancellationToken cancellationToken = default);
    
    // Reaction methods
    Task<ArticleReactionSummaryDto> ToggleReactionAsync(Guid articleId, Guid userId, string reactionType, CancellationToken cancellationToken = default);
    Task<ArticleReactionSummaryDto> GetMyReactionAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default);
}