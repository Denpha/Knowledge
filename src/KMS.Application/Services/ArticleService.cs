using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Interaction;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Interaction;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Enums;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services;

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _articleRepository;
    private readonly IRepository<ArticleVersion> _articleVersionRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<ArticleReaction> _reactionRepository;
    private readonly IPublishWorkflowService _publishWorkflowService;
    private readonly INotificationService _notificationService;
    private readonly IDistributedCache _cache;

    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public ArticleService(
        IArticleRepository articleRepository,
        IRepository<ArticleVersion> articleVersionRepository,
        IRepository<Category> categoryRepository,
        IRepository<Tag> tagRepository,
        IRepository<ArticleReaction> reactionRepository,
        IPublishWorkflowService publishWorkflowService,
        INotificationService notificationService,
        IDistributedCache cache)
    {
        _articleRepository = articleRepository;
        _articleVersionRepository = articleVersionRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _reactionRepository = reactionRepository;
        _publishWorkflowService = publishWorkflowService;
        _notificationService = notificationService;
        _cache = cache;
    }

    public async Task<ArticleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(id, cancellationToken);
        return article?.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetBySlugAsync(slug, cancellationToken);
        return article?.Adapt<ArticleDto>();
    }

    public async Task<PaginatedResult<ArticleDto>> SearchAsync(ArticleSearchParams searchParams, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"articles:list:{searchParams.PageNumber}:{searchParams.PageSize}:{searchParams.Status}";
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<PaginatedResult<ArticleDto>>(cached)!;
        }

        // For now, return a simple implementation
        var articles = await _articleRepository.GetPublishedArticlesAsync(cancellationToken);
        
        var articleDtos = articles
            .Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .Select(a => a.Adapt<ArticleDto>())
            .ToList();

        var result = new PaginatedResult<ArticleDto>
        {
            Items = articleDtos,
            PageNumber = searchParams.PageNumber,
            PageSize = searchParams.PageSize,
            TotalCount = articles.Count()
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), _cacheOptions, cancellationToken);
        return result;
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleDto createDto, Guid authorId, CancellationToken cancellationToken = default)
    {
        // Validate category exists
        var category = await _categoryRepository.GetByIdAsync(createDto.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new ArgumentException($"Category with ID {createDto.CategoryId} not found.");
        }

        // Generate slug from title
        var slug = GenerateSlug(createDto.Title);
        
        // Check if slug exists (simplified)
        var existingArticle = await _articleRepository.GetBySlugAsync(slug, cancellationToken);
        if (existingArticle != null)
        {
            slug = $"{slug}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        // Create article entity
        var article = new KnowledgeArticle
        {
            Title = createDto.Title,
            TitleEn = createDto.TitleEn,
            Content = createDto.Content,
            ContentEn = createDto.ContentEn,
            Summary = createDto.Summary,
            SummaryEn = createDto.SummaryEn,
            KeywordsEn = createDto.KeywordsEn,
            Slug = slug,
            CategoryId = createDto.CategoryId,
            AuthorId = authorId,
            Status = ArticleStatus.Draft,
            Visibility = Visibility.Internal,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add tags if provided
        if (createDto.TagIds.Any())
        {
            var tags = await _tagRepository.GetAllAsync(cancellationToken);
            var selectedTags = tags.Where(t => createDto.TagIds.Contains(t.Id)).ToList();
            
            foreach (var tag in selectedTags)
            {
                article.ArticleTags.Add(new ArticleTag
                {
                    ArticleId = article.Id,
                    TagId = tag.Id
                });
            }
        }

        await _articleRepository.AddAsync(article, cancellationToken);
        await _articleRepository.SaveChangesAsync(cancellationToken);

        await InvalidateArticleListCacheAsync(cancellationToken);

        // Reload with includes
        var createdArticle = await _articleRepository.GetByIdAsync(article.Id, cancellationToken);
        return createdArticle!.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto> UpdateAsync(Guid id, UpdateArticleDto updateDto, Guid updatedById, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(id, cancellationToken);
        if (article == null)
        {
            throw new KeyNotFoundException($"Article with ID {id} not found.");
        }

        // Check if article can be updated (not published or archived)
        if (article.Status == ArticleStatus.Published || article.Status == ArticleStatus.Archived)
        {
            throw new InvalidOperationException($"Article with status {article.Status} cannot be updated.");
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(updateDto.Title))
        {
            article.Title = updateDto.Title;
            
            // Update slug if title changed
            var newSlug = GenerateSlug(updateDto.Title);
            if (newSlug != article.Slug)
            {
                // Check if slug already exists
                var existingArticle = await _articleRepository.GetBySlugAsync(newSlug, cancellationToken);
                article.Slug = existingArticle != null ? $"{newSlug}-{DateTime.UtcNow:yyyyMMddHHmmss}" : newSlug;
            }
        }

        if (updateDto.TitleEn != null) article.TitleEn = updateDto.TitleEn;
        if (!string.IsNullOrEmpty(updateDto.Content)) article.Content = updateDto.Content;
        if (updateDto.ContentEn != null) article.ContentEn = updateDto.ContentEn;
        if (!string.IsNullOrEmpty(updateDto.Summary)) article.Summary = updateDto.Summary;
        if (updateDto.SummaryEn != null) article.SummaryEn = updateDto.SummaryEn;
        if (updateDto.KeywordsEn != null) article.KeywordsEn = updateDto.KeywordsEn;
        
        if (updateDto.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(updateDto.CategoryId.Value, cancellationToken);
            if (category == null)
            {
                throw new ArgumentException($"Category with ID {updateDto.CategoryId.Value} not found.");
            }
            article.CategoryId = updateDto.CategoryId.Value;
        }

        article.UpdatedAt = DateTime.UtcNow;

        // Update tags if provided
        if (updateDto.TagIds != null)
        {
            article.ArticleTags.Clear();
            
            var tags = await _tagRepository.GetAllAsync(cancellationToken);
            var selectedTags = tags.Where(t => updateDto.TagIds.Contains(t.Id)).ToList();
            
            foreach (var tag in selectedTags)
            {
                article.ArticleTags.Add(new ArticleTag
                {
                    ArticleId = article.Id,
                    TagId = tag.Id
                });
            }
        }

        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync(cancellationToken);

        await InvalidateArticleListCacheAsync(cancellationToken);

        // Reload with includes
        var updatedArticle = await _articleRepository.GetByIdAsync(id, cancellationToken);
        return updatedArticle!.Adapt<ArticleDto>();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(id, cancellationToken);
        if (article == null)
        {
            return false;
        }

        // For now, just remove the article
        _articleRepository.Remove(article);
        await _articleRepository.SaveChangesAsync(cancellationToken);

        await InvalidateArticleListCacheAsync(cancellationToken);

        return true;
    }

    public async Task<ArticleDto> PublishAsync(Guid id, Guid publisherId, string? reviewNotes = null, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(id, cancellationToken);
        if (article == null)
        {
            throw new KeyNotFoundException($"Article with ID {id} not found.");
        }

        // Check if article can be published
        if (article.Status == ArticleStatus.Published)
        {
            throw new InvalidOperationException("Article is already published.");
        }

        if (article.Status == ArticleStatus.Archived)
        {
            throw new InvalidOperationException("Archived articles cannot be published.");
        }

        // Check if user can publish directly or needs review
        var canPublishDirectly = await _publishWorkflowService.CanPublishDirectlyAsync(publisherId, cancellationToken);
        var requiresReview = await _publishWorkflowService.RequiresReviewAsync(publisherId, cancellationToken);

        if (requiresReview && !canPublishDirectly)
        {
            // Researcher needs review before publishing
            // Move article to UnderReview status
            article.Status = ArticleStatus.UnderReview;
            article.ReviewerId = null; // Will be assigned by reviewer
            article.ReviewRequestedAt = DateTime.UtcNow;
            article.UpdatedAt = DateTime.UtcNow;

            _articleRepository.Update(article);
            await _articleRepository.SaveChangesAsync(cancellationToken);

            // Notify potential reviewers
            await NotifyReviewersAsync(article, cancellationToken);

            await InvalidateArticleListCacheAsync(cancellationToken);

            // Reload with includes
            var underReviewArticle = await _articleRepository.GetByIdAsync(id, cancellationToken);
            return underReviewArticle!.Adapt<ArticleDto>();
        }
        else
        {
            // Admin/Faculty can publish directly
            // Update article status
            article.Status = ArticleStatus.Published;
            article.PublishedAt = DateTime.UtcNow;
            article.ReviewerId = publisherId; // Self-approved
            article.UpdatedAt = DateTime.UtcNow;

            _articleRepository.Update(article);
            await _articleRepository.SaveChangesAsync(cancellationToken);

            await InvalidateArticleListCacheAsync(cancellationToken);

            // Reload with includes
            var publishedArticle = await _articleRepository.GetByIdAsync(id, cancellationToken);
            return publishedArticle!.Adapt<ArticleDto>();
        }
    }

    public async Task<ArticleDto> ArchiveAsync(Guid id, Guid archivedById, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(id, cancellationToken);
        if (article == null)
        {
            throw new KeyNotFoundException($"Article with ID {id} not found.");
        }

        article.Status = ArticleStatus.Archived;
        article.UpdatedAt = DateTime.UtcNow;

        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync(cancellationToken);

        // Reload with includes
        var archivedArticle = await _articleRepository.GetByIdAsync(id, cancellationToken);
        return archivedArticle!.Adapt<ArticleDto>();
    }

    public async Task<bool> CheckSlugAvailabilityAsync(string slug, Guid? excludeArticleId = null, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetBySlugAsync(slug, cancellationToken);
        if (article == null)
        {
            return true;
        }
        
        return excludeArticleId.HasValue && article.Id == excludeArticleId.Value;
    }

    public async Task<List<ArticleVersionDto>> GetVersionsAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        var versions = await _articleVersionRepository.GetAllAsync(cancellationToken);
        var articleVersions = versions
            .Where(v => v.ArticleId == articleId)
            .OrderByDescending(v => v.CreatedAt)
            .ToList();

        return articleVersions.Adapt<List<ArticleVersionDto>>();
    }

    public async Task<ArticleVersionDto> GetVersionAsync(Guid articleId, int versionNumber, CancellationToken cancellationToken = default)
    {
        var versions = await _articleVersionRepository.GetAllAsync(cancellationToken);
        var version = versions
            .FirstOrDefault(v => v.ArticleId == articleId && v.VersionNumber == versionNumber);

        if (version == null)
        {
            throw new KeyNotFoundException($"Version {versionNumber} of article {articleId} not found.");
        }

        return version.Adapt<ArticleVersionDto>();
    }

    public async Task<ArticleDto> RestoreVersionAsync(Guid articleId, int versionNumber, Guid restoredById, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
        if (article == null)
        {
            throw new KeyNotFoundException($"Article with ID {articleId} not found.");
        }

        var version = await GetVersionAsync(articleId, versionNumber, cancellationToken);

        // Restore article content from version
        article.Title = version.Title;
        article.Content = version.Content;
        article.ContentEn = version.ContentEn;
        article.Summary = version.Summary;
        article.UpdatedAt = DateTime.UtcNow;

        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync(cancellationToken);

        // Create version for restore
        await CreateArticleVersion(article, restoredById, $"Restored from version {versionNumber}", cancellationToken);

        // Reload with includes
        var restoredArticle = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
        return restoredArticle!.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto> ReviewApproveAsync(Guid articleId, Guid reviewerId, string? reviewNotes = null, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
        if (article == null)
        {
            throw new KeyNotFoundException($"Article with ID {articleId} not found.");
        }

        // Check if article is under review
        if (article.Status != ArticleStatus.UnderReview)
        {
            throw new InvalidOperationException($"Article is not under review. Current status: {article.Status}");
        }

        // Check if user can approve publications
        var canApprove = await _publishWorkflowService.CanApprovePublicationAsync(reviewerId, articleId, cancellationToken);
        if (!canApprove)
        {
            throw new UnauthorizedAccessException("User does not have permission to approve publications.");
        }

        // Approve the article
        article.Status = ArticleStatus.Published;
        article.ReviewerId = reviewerId;
        article.PublishedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;

        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync(cancellationToken);

        // Notify author about approval
        await _notificationService.CreateAsync(new Application.DTOs.Logging.CreateNotificationDto
        {
            UserId = article.AuthorId,
            Type = "ArticlePublished",
            Title = "บทความของคุณได้รับการเผยแพร่แล้ว",
            Message = $"บทความ \"{article.Title}\" ได้รับการอนุมัติและเผยแพร่แล้ว",
            ReferenceUrl = $"/articles/{article.Slug}"
        }, reviewerId, cancellationToken);

        // Reload with includes
        var publishedArticle = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
        return publishedArticle!.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto> ReviewRejectAsync(Guid articleId, Guid reviewerId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
        if (article == null)
        {
            throw new KeyNotFoundException($"Article with ID {articleId} not found.");
        }

        // Check if article is under review
        if (article.Status != ArticleStatus.UnderReview)
        {
            throw new InvalidOperationException($"Article is not under review. Current status: {article.Status}");
        }

        // Check if user can review articles
        var canReview = await _publishWorkflowService.CanReviewArticlesAsync(reviewerId, cancellationToken);
        if (!canReview)
        {
            throw new UnauthorizedAccessException("User does not have permission to review articles.");
        }

        // Reject the article (move back to Draft)
        article.Status = ArticleStatus.Draft;
        article.ReviewerId = reviewerId;
        article.UpdatedAt = DateTime.UtcNow;

        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync(cancellationToken);

        // Notify author about rejection
        await _notificationService.CreateAsync(new Application.DTOs.Logging.CreateNotificationDto
        {
            UserId = article.AuthorId,
            Type = "ArticleRejected",
            Title = "บทความของคุณไม่ผ่านการตรวจสอบ",
            Message = $"บทความ \"{article.Title}\" ไม่ผ่านการตรวจสอบด้วยเหตุผล: {rejectionReason}",
            ReferenceUrl = $"/articles/{article.Slug}"
        }, reviewerId, cancellationToken);

        // Reload with includes
        var rejectedArticle = await _articleRepository.GetByIdAsync(articleId, cancellationToken);
        return rejectedArticle!.Adapt<ArticleDto>();
    }

    public async Task<List<ArticleDto>> GetArticlesRequiringReviewAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        // Check if user can review articles
        var canReview = await _publishWorkflowService.CanReviewArticlesAsync(reviewerId, cancellationToken);
        if (!canReview)
        {
            return new List<ArticleDto>();
        }

        // Get all articles under review
        var articles = await _articleRepository.GetAllAsync(cancellationToken);
        var articlesRequiringReview = articles
            .Where(a => a.Status == ArticleStatus.UnderReview)
            .ToList();

        return articlesRequiringReview.Adapt<List<ArticleDto>>();
    }

    public async Task<List<ArticleDto>> GetMyArticlesUnderReviewAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        // Get author's articles that are under review
        var articles = await _articleRepository.GetAllAsync(cancellationToken);
        var myArticlesUnderReview = articles
            .Where(a => a.AuthorId == authorId && a.Status == ArticleStatus.UnderReview)
            .ToList();

        return myArticlesUnderReview.Adapt<List<ArticleDto>>();
    }

    public async Task<ArticleReactionSummaryDto> ToggleReactionAsync(Guid articleId, Guid userId, string reactionType, CancellationToken cancellationToken = default)
    {
        var existing = (await _reactionRepository.FindAsync(
            r => r.ArticleId == articleId && r.UserId == userId && r.ReactionType == reactionType,
            cancellationToken)).FirstOrDefault();

        if (existing != null)
        {
            _reactionRepository.Remove(existing);
        }
        else
        {
            await _reactionRepository.AddAsync(new ArticleReaction
            {
                Id = Guid.NewGuid(),
                ArticleId = articleId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        await _reactionRepository.SaveChangesAsync(cancellationToken);

        return await BuildReactionSummaryAsync(articleId, userId, cancellationToken);
    }

    public async Task<ArticleReactionSummaryDto> GetMyReactionAsync(Guid articleId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await BuildReactionSummaryAsync(articleId, userId, cancellationToken);
    }

    private async Task<ArticleReactionSummaryDto> BuildReactionSummaryAsync(Guid articleId, Guid userId, CancellationToken cancellationToken)
    {
        var allReactions = await _reactionRepository.FindAsync(r => r.ArticleId == articleId, cancellationToken);
        var list = allReactions.ToList();
        return new ArticleReactionSummaryDto
        {
            ArticleId = articleId,
            LikeCount = list.Count(r => r.ReactionType == "Like"),
            BookmarkCount = list.Count(r => r.ReactionType == "Bookmark"),
            ShareCount = list.Count(r => r.ReactionType == "Share"),
            UserLiked = list.Any(r => r.UserId == userId && r.ReactionType == "Like"),
            UserBookmarked = list.Any(r => r.UserId == userId && r.ReactionType == "Bookmark"),
            UserShared = list.Any(r => r.UserId == userId && r.ReactionType == "Share")
        };
    }

    private async Task NotifyReviewersAsync(KnowledgeArticle article, CancellationToken cancellationToken = default)
    {
        try
        {
            var reviewers = await _publishWorkflowService.GetReviewersAsync(cancellationToken);
            
            foreach (var reviewerId in reviewers)
            {
                await _notificationService.CreateAsync(new Application.DTOs.Logging.CreateNotificationDto
                {
                    UserId = reviewerId,
                    Type = "ReviewRequested",
                    Title = "มีบทความรอการตรวจสอบ",
                    Message = $"บทความ \"{article.Title}\" รอการตรวจสอบก่อนเผยแพร่",
                    ReferenceUrl = $"/articles/{article.Slug}"
                }, article.AuthorId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the publish operation
            Console.WriteLine($"Failed to notify reviewers: {ex.Message}");
        }
    }

    private async Task CreateArticleVersion(KnowledgeArticle article, Guid editedById, string changeNote, CancellationToken cancellationToken)
    {
        // Get current version number
        var versions = await _articleVersionRepository.GetAllAsync(cancellationToken);
        var currentVersion = versions
            .Where(v => v.ArticleId == article.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        var versionNumber = currentVersion?.VersionNumber + 1 ?? 1;

        var version = new ArticleVersion
        {
            ArticleId = article.Id,
            VersionNumber = versionNumber,
            Title = article.Title,
            Content = article.Content,
            ContentEn = article.ContentEn,
            Summary = article.Summary,
            EditedById = editedById,
            ChangeNote = changeNote,
            CreatedAt = DateTime.UtcNow
        };

        await _articleVersionRepository.AddAsync(version, cancellationToken);
        await _articleVersionRepository.SaveChangesAsync(cancellationToken);
    }

    private Task InvalidateArticleListCacheAsync(CancellationToken cancellationToken)
    {
        // Cache keys use page/pageSize/status — remove known common entries.
        // Since IDistributedCache has no pattern-delete, we use a sentinel key to signal staleness.
        // Simple approach: remove the first-page defaults which are the most frequently cached.
        var commonKeys = new[]
        {
            "articles:list:1:10:",
            "articles:list:1:10:Published",
            "articles:list:1:20:",
            "articles:list:1:20:Published"
        };
        return Task.WhenAll(commonKeys.Select(k => _cache.RemoveAsync(k, cancellationToken)));
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLower()
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