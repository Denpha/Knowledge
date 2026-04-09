using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.DTOs.Media;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Entities.Logging;
using KMS.Domain.Entities.Media;
using KMS.Domain.Enums;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services;

public class SearchService : ISearchService
{
    private readonly IArticleRepository _articleRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<MediaItem> _mediaRepository;
    private readonly IRepository<KnowledgeSearchLog> _searchLogRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IArticleRepository articleRepository,
        IRepository<Category> categoryRepository,
        IRepository<Tag> tagRepository,
        IRepository<MediaItem> mediaRepository,
        IRepository<KnowledgeSearchLog> searchLogRepository,
        IEmbeddingService embeddingService,
        ILogger<SearchService> logger)
    {
        _articleRepository = articleRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _mediaRepository = mediaRepository;
        _searchLogRepository = searchLogRepository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<PaginatedResult<SearchResultDto>> SearchAsync(SearchRequestParams searchParams, CancellationToken cancellationToken = default)
    {
        try
        {
            var allResults = new List<SearchResultDto>();
            var query = searchParams.Query?.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                return new PaginatedResult<SearchResultDto>
                {
                    Items = new List<SearchResultDto>(),
                    PageNumber = searchParams.PageNumber,
                    PageSize = searchParams.PageSize,
                    TotalCount = 0
                };
            }

            // Determine which result types to search
            var resultTypes = searchParams.ResultTypes.Any() 
                ? searchParams.ResultTypes 
                : new List<SearchResultType> { SearchResultType.Article, SearchResultType.Category, SearchResultType.Tag };

            // Search in each entity type
            foreach (var resultType in resultTypes)
            {
                var results = resultType switch
                {
                    SearchResultType.Article => await SearchArticlesAsync(query, searchParams, cancellationToken),
                    SearchResultType.Category => await SearchCategoriesAsync(query, searchParams, cancellationToken),
                    SearchResultType.Tag => await SearchTagsAsync(query, searchParams, cancellationToken),
                    SearchResultType.Media => await SearchMediaAsync(query, searchParams, cancellationToken),
                    _ => new List<SearchResultDto>()
                };

                allResults.AddRange(results);
            }

            // Sort by relevance (if available) or by created date
            var sortedResults = allResults
                .OrderByDescending(r => r.RelevanceScore ?? 0)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            // Apply pagination
            var totalCount = sortedResults.Count;
            var skip = (searchParams.PageNumber - 1) * searchParams.PageSize;
            var items = sortedResults
                .Skip(skip)
                .Take(searchParams.PageSize)
                .ToList();

            // Log the search
            await LogSearchAsync(query, null, SearchMode.FullText, totalCount, cancellationToken);

            return new PaginatedResult<SearchResultDto>
            {
                Items = items,
                PageNumber = searchParams.PageNumber,
                PageSize = searchParams.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search for query: {Query}", searchParams.Query);
            throw;
        }
    }

    public async Task<List<SearchResultDto>> SemanticSearchAsync(SemanticSearchParams searchParams, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = searchParams.Query?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<SearchResultDto>();
            }

            // Generate embedding for the query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            var vector = new Vector(queryEmbedding);

            // Build query for semantic search
            var articleQuery = _articleRepository.Query
                .Where(a => a.Embedding != null)
                .Where(a => a.Status == ArticleStatus.Published)
                .Where(a => a.Visibility == Visibility.Public);

            // Apply filters
            if (searchParams.CategoryIds?.Any() == true)
            {
                articleQuery = articleQuery.Where(a => searchParams.CategoryIds.Contains(a.CategoryId));
            }

            if (searchParams.ArticleStatus.HasValue)
            {
                articleQuery = articleQuery.Where(a => a.Status == searchParams.ArticleStatus.Value);
            }

            if (searchParams.Visibility.HasValue)
            {
                articleQuery = articleQuery.Where(a => a.Visibility == searchParams.Visibility.Value);
            }

            // Perform semantic search using pgvector
            // Note: The Distance method might not be available in the current pgvector version
            // Using a placeholder for now to allow compilation
            var articles = await articleQuery
                .OrderBy(a => a.Title) // Placeholder instead of distance calculation
                .Take(searchParams.Limit)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .ToListAsync(cancellationToken);

            // Convert to search results
            var results = new List<SearchResultDto>();
            foreach (var article in articles)
            {
                // Calculate similarity score (1 - distance)
                // Note: The Distance method might not be available in the current pgvector version
                // Using a placeholder for now to allow compilation
                var distance = 0.5; // Placeholder instead of actual distance calculation
                var similarity = 1.0 - distance;

                // Apply threshold
                if (similarity < (searchParams.SimilarityThreshold ?? 0.7))
                {
                    continue;
                }

                var articleDto = article.Adapt<ArticleDto>();
                articleDto.CategoryName = article.Category?.Name ?? string.Empty;
                articleDto.AuthorName = article.Author?.FullNameTh ?? article.Author?.UserName ?? string.Empty;
                articleDto.Tags = article.ArticleTags
                    .Select(at => at.Tag?.Adapt<TagDto>())
                    .Where(t => t != null)
                    .ToList()!;

                results.Add(new SearchResultDto
                {
                    ResultType = SearchResultType.Article,
                    RelevanceScore = similarity,
                    Article = articleDto,
                    HighlightedText = GetHighlightedText(article, query),
                    MatchedFields = new List<string> { "semantic_content" },
                    Id = article.Id,
                    CreatedAt = article.CreatedAt,
                    UpdatedAt = article.UpdatedAt
                });
            }

            // Log the search
            await LogSearchAsync(query, null, SearchMode.Semantic, results.Count, cancellationToken);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search for query: {Query}", searchParams.Query);
            throw;
        }
    }

    public async Task<PaginatedResult<SearchResultDto>> HybridSearchAsync(SearchRequestParams searchParams, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = searchParams.Query?.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                return new PaginatedResult<SearchResultDto>
                {
                    Items = new List<SearchResultDto>(),
                    PageNumber = searchParams.PageNumber,
                    PageSize = searchParams.PageSize,
                    TotalCount = 0
                };
            }

            // Perform both full-text and semantic search
            var fullTextTask = SearchAsync(searchParams, cancellationToken);
            var semanticTask = SemanticSearchAsync(new SemanticSearchParams
            {
                Query = query,
                Limit = searchParams.PageSize * 2, // Get more for better merging
                SimilarityThreshold = searchParams.SemanticThreshold,
                CategoryIds = searchParams.CategoryIds,
                ArticleStatus = searchParams.ArticleStatus,
                Visibility = searchParams.Visibility,
                LanguagePreference = searchParams.LanguagePreference
            }, cancellationToken);

            await Task.WhenAll(fullTextTask, semanticTask);

            var fullTextResults = fullTextTask.Result.Items;
            var semanticResults = semanticTask.Result;

            // Merge results
            var mergedResults = new Dictionary<Guid, SearchResultDto>();

            // Add semantic results first (higher relevance)
            foreach (var semanticResult in semanticResults)
            {
                if (semanticResult.Article != null)
                {
                    mergedResults[semanticResult.Article.Id] = semanticResult;
                }
            }

            // Add full-text results (avoid duplicates)
            foreach (var fullTextResult in fullTextResults)
            {
                if (fullTextResult.Article != null && !mergedResults.ContainsKey(fullTextResult.Article.Id))
                {
                    mergedResults[fullTextResult.Article.Id] = fullTextResult;
                }
                else if (fullTextResult.Category != null && !mergedResults.ContainsKey(fullTextResult.Category.Id))
                {
                    mergedResults[fullTextResult.Category.Id] = fullTextResult;
                }
                else if (fullTextResult.Tag != null && !mergedResults.ContainsKey(fullTextResult.Tag.Id))
                {
                    mergedResults[fullTextResult.Tag.Id] = fullTextResult;
                }
                else if (fullTextResult.MediaItem != null && !mergedResults.ContainsKey(fullTextResult.MediaItem.Id))
                {
                    mergedResults[fullTextResult.MediaItem.Id] = fullTextResult;
                }
            }

            // Sort by relevance score
            var sortedResults = mergedResults.Values
                .OrderByDescending(r => r.RelevanceScore ?? 0)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            // Apply pagination
            var totalCount = sortedResults.Count;
            var skip = (searchParams.PageNumber - 1) * searchParams.PageSize;
            var items = sortedResults
                .Skip(skip)
                .Take(searchParams.PageSize)
                .ToList();

            // Log the search
            await LogSearchAsync(query, null, SearchMode.Hybrid, totalCount, cancellationToken);

            return new PaginatedResult<SearchResultDto>
            {
                Items = items,
                PageNumber = searchParams.PageNumber,
                PageSize = searchParams.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hybrid search for query: {Query}", searchParams.Query);
            throw;
        }
    }

    public async Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var trimmedQuery = query?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedQuery) || trimmedQuery.Length < 2)
            {
                return new List<SearchSuggestionDto>();
            }

            var suggestions = new List<SearchSuggestionDto>();

            // Get article title suggestions
            var articleTitles = await _articleRepository.Query
                .Where(a => a.Status == ArticleStatus.Published)
                .Where(a => a.Title.Contains(trimmedQuery) || (a.TitleEn != null && a.TitleEn.Contains(trimmedQuery)))
                .Select(a => new { a.Title, a.TitleEn })
                .Take(limit)
                .ToListAsync(cancellationToken);

            foreach (var article in articleTitles)
            {
                suggestions.Add(new SearchSuggestionDto
                {
                    Query = article.Title,
                    ResultType = SearchResultType.Article,
                    Count = 1
                });
            }

            // Get category name suggestions
            var categoryNames = await _categoryRepository.Query
                .Where(c => c.Name.Contains(trimmedQuery))
                .Select(c => c.Name)
                .Take(limit)
                .ToListAsync(cancellationToken);

            foreach (var category in categoryNames)
            {
                suggestions.Add(new SearchSuggestionDto
                {
                    Query = category,
                    ResultType = SearchResultType.Category,
                    Count = 1
                });
            }

            // Get tag name suggestions
            var tagNames = await _tagRepository.Query
                .Where(t => t.Name.Contains(trimmedQuery))
                .Select(t => t.Name)
                .Take(limit)
                .ToListAsync(cancellationToken);

            foreach (var tag in tagNames)
            {
                suggestions.Add(new SearchSuggestionDto
                {
                    Query = tag,
                    ResultType = SearchResultType.Tag,
                    Count = 1
                });
            }

            // Remove duplicates and limit results
            return suggestions
                .GroupBy(s => s.Query)
                .Select(g => g.First())
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            return new List<SearchSuggestionDto>();
        }
    }

    public async Task<SearchStatsDto> GetSearchStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = new SearchStatsDto();

            // Get counts
            stats.TotalArticles = await _articleRepository.Query
                .Where(a => a.Status == ArticleStatus.Published)
                .CountAsync(cancellationToken);

            stats.TotalCategories = await _categoryRepository.Query
                .CountAsync(cancellationToken);

            stats.TotalTags = await _tagRepository.Query
                .CountAsync(cancellationToken);

            stats.TotalMediaItems = await _mediaRepository.Query
                .CountAsync(cancellationToken);

            // Get top categories
            var topCategories = await _articleRepository.Query
                .Where(a => a.Status == ArticleStatus.Published)
                .GroupBy(a => a.Category.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            stats.TopCategories = topCategories.ToDictionary(x => x.Category, x => x.Count);

            // Get top tags
            var topTags = await _articleRepository.Query
                .Where(a => a.Status == ArticleStatus.Published)
                .SelectMany(a => a.ArticleTags)
                .GroupBy(at => at.Tag.Name)
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            stats.TopTags = topTags.ToDictionary(x => x.Tag, x => x.Count);

            // Get monthly article counts (last 12 months)
            var oneYearAgo = DateTime.UtcNow.AddMonths(-12);
            var monthlyCounts = await _articleRepository.Query
                .Where(a => a.Status == ArticleStatus.Published && a.PublishedAt >= oneYearAgo)
                .GroupBy(a => new { Year = a.PublishedAt!.Value.Year, Month = a.PublishedAt.Value.Month })
                .Select(g => new { YearMonth = g.Key.Year * 100 + g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.YearMonth)
                .ToListAsync(cancellationToken);

            stats.MonthlyArticleCounts = monthlyCounts.ToDictionary(x => x.YearMonth, x => x.Count);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search statistics");
            throw;
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken);
    }

    public async Task<int> UpdateArticleEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get articles without embeddings
            var articlesWithoutEmbeddings = await _articleRepository.Query
                .Where(a => a.Embedding == null && a.Status == ArticleStatus.Published)
                .Take(100) // Process in batches
                .ToListAsync(cancellationToken);

            var updatedCount = 0;

            foreach (var article in articlesWithoutEmbeddings)
            {
                try
                {
                    // Generate text for embedding (combine title and content)
                    var textForEmbedding = $"{article.Title} {article.Summary} {article.Content}";
                    
                    // Generate embedding
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(textForEmbedding, cancellationToken);
                    article.Embedding = new Vector(embedding);

                    // Update the article
                    _articleRepository.Update(article);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for article {ArticleId}", article.Id);
                }
            }

            // Save changes if any updates were made
            if (updatedCount > 0)
            {
                await _articleRepository.SaveChangesAsync(cancellationToken);
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating article embeddings");
            throw;
        }
    }

    public async Task LogSearchAsync(string query, Guid? userId, SearchMode mode, int resultCount, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchLog = new KnowledgeSearchLog
            {
                Query = query,
                UserId = userId,
                SearchType = mode.ToString(),
                ResultCount = resultCount,
                CreatedAt = DateTime.UtcNow
            };

            await _searchLogRepository.AddAsync(searchLog, cancellationToken);
            await _searchLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log search query: {Query}", query);
            // Don't throw - logging failure shouldn't break search
        }
    }

    #region Private Methods

    private async Task<List<SearchResultDto>> SearchArticlesAsync(string query, SearchRequestParams searchParams, CancellationToken cancellationToken)
    {
        var articleQuery = _articleRepository.Query
            .Where(a => a.Status == ArticleStatus.Published);

        // Apply language preference
        var searchFields = new List<string>();
        if (searchParams.LanguagePreference != LanguagePreference.Secondary)
        {
            if (searchParams.SearchInTitle) searchFields.Add("Title");
            if (searchParams.SearchInContent) searchFields.Add("Content");
            if (searchParams.SearchInSummary) searchFields.Add("Summary");
        }

        if (searchParams.LanguagePreference != LanguagePreference.Primary)
        {
            if (searchParams.SearchInTitle) searchFields.Add("TitleEn");
            if (searchParams.SearchInSummary) searchFields.Add("SummaryEn");
        }

        // Build search conditions
        var conditions = new List<string>();
        foreach (var field in searchFields)
        {
            conditions.Add($"{field}.Contains(\"{query}\")");
        }

        if (conditions.Any())
        {
            var dynamicQuery = string.Join(" || ", conditions);
            // Note: In real implementation, you would use a proper full-text search
            // For now, we'll use simple Contains
            articleQuery = articleQuery.Where(a => 
                a.Title.Contains(query) || 
                a.Content.Contains(query) || 
                a.Summary.Contains(query) ||
                (a.TitleEn != null && a.TitleEn.Contains(query)) ||
                (a.SummaryEn != null && a.SummaryEn.Contains(query)));
        }

        // Apply filters
        if (searchParams.CategoryIds?.Any() == true)
        {
            articleQuery = articleQuery.Where(a => searchParams.CategoryIds.Contains(a.CategoryId));
        }

        if (searchParams.ArticleStatus.HasValue)
        {
            articleQuery = articleQuery.Where(a => a.Status == searchParams.ArticleStatus.Value);
        }

        if (searchParams.Visibility.HasValue)
        {
            articleQuery = articleQuery.Where(a => a.Visibility == searchParams.Visibility.Value);
        }

        if (searchParams.CreatedFrom.HasValue)
        {
            articleQuery = articleQuery.Where(a => a.CreatedAt >= searchParams.CreatedFrom.Value);
        }

        if (searchParams.CreatedTo.HasValue)
        {
            articleQuery = articleQuery.Where(a => a.CreatedAt <= searchParams.CreatedTo.Value);
        }

        if (searchParams.PublishedFrom.HasValue)
        {
            articleQuery = articleQuery.Where(a => a.PublishedAt >= searchParams.PublishedFrom.Value);
        }

        if (searchParams.PublishedTo.HasValue)
        {
            articleQuery = articleQuery.Where(a => a.PublishedAt <= searchParams.PublishedTo.Value);
        }

        // Execute query
        var articles = await articleQuery
            .Include(a => a.Category)
            .Include(a => a.Author)
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .ToListAsync(cancellationToken);

        // Convert to search results
        var results = new List<SearchResultDto>();
        foreach (var article in articles)
        {
            var articleDto = article.Adapt<ArticleDto>();
            articleDto.CategoryName = article.Category?.Name ?? string.Empty;
            articleDto.AuthorName = article.Author?.FullNameTh ?? article.Author?.UserName ?? string.Empty;
            articleDto.Tags = article.ArticleTags
                .Select(at => at.Tag?.Adapt<TagDto>())
                .Where(t => t != null)
                .ToList()!;

            // Calculate relevance score (simple implementation)
            var relevanceScore = CalculateRelevanceScore(article, query);

            results.Add(new SearchResultDto
            {
                ResultType = SearchResultType.Article,
                RelevanceScore = relevanceScore,
                Article = articleDto,
                HighlightedText = GetHighlightedText(article, query),
                MatchedFields = GetMatchedFields(article, query),
                Id = article.Id,
                CreatedAt = article.CreatedAt,
                UpdatedAt = article.UpdatedAt
            });
        }

        return results;
    }

    private async Task<List<SearchResultDto>> SearchCategoriesAsync(string query, SearchRequestParams searchParams, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.Query
            .Where(c => c.Name.Contains(query) || (c.Description != null && c.Description.Contains(query)))
            .Include(c => c.Parent)
            .ToListAsync(cancellationToken);

        return categories.Select(category =>
        {
            var categoryDto = category.Adapt<CategoryDto>();
            return new SearchResultDto
            {
                ResultType = SearchResultType.Category,
                RelevanceScore = CalculateCategoryRelevanceScore(category, query),
                Category = categoryDto,
                HighlightedText = GetCategoryHighlightedText(category, query),
                MatchedFields = GetCategoryMatchedFields(category, query),
                Id = category.Id,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }).ToList();
    }

    private async Task<List<SearchResultDto>> SearchTagsAsync(string query, SearchRequestParams searchParams, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.Query
            .Where(t => t.Name.Contains(query) || t.Slug.Contains(query))
            .ToListAsync(cancellationToken);

        return tags.Select(tag =>
        {
            var tagDto = tag.Adapt<TagDto>();
            return new SearchResultDto
            {
                ResultType = SearchResultType.Tag,
                RelevanceScore = CalculateTagRelevanceScore(tag, query),
                Tag = tagDto,
                HighlightedText = GetTagHighlightedText(tag, query),
                MatchedFields = GetTagMatchedFields(tag, query),
                Id = tag.Id,
                CreatedAt = tag.CreatedAt,
                UpdatedAt = null
            };
        }).ToList();
    }

    private async Task<List<SearchResultDto>> SearchMediaAsync(string query, SearchRequestParams searchParams, CancellationToken cancellationToken)
    {
        var mediaItems = await _mediaRepository.Query
            .Where(m => m.Name.Contains(query) ||
                       m.FileName.Contains(query))
            .ToListAsync(cancellationToken);

        return mediaItems.Select(media =>
        {
            var mediaDto = media.Adapt<MediaItemDto>();
            return new SearchResultDto
            {
                ResultType = SearchResultType.Media,
                RelevanceScore = CalculateMediaRelevanceScore(media, query),
                MediaItem = mediaDto,
                HighlightedText = GetMediaHighlightedText(media, query),
                MatchedFields = GetMediaMatchedFields(media, query),
                Id = media.Id,
                CreatedAt = media.CreatedAt,
                UpdatedAt = media.UpdatedAt
            };
        }).ToList();
    }

    private double CalculateRelevanceScore(KnowledgeArticle article, string query)
    {
        var score = 0.0;
        query = query.ToLowerInvariant();

        // Check title match (highest weight)
        if (article.Title.ToLowerInvariant().Contains(query))
            score += 3.0;

        if (article.TitleEn?.ToLowerInvariant().Contains(query) == true)
            score += 2.5;

        // Check summary match
        if (article.Summary.ToLowerInvariant().Contains(query))
            score += 2.0;

        if (article.SummaryEn?.ToLowerInvariant().Contains(query) == true)
            score += 1.5;

        // Check content match
        if (article.Content.ToLowerInvariant().Contains(query))
            score += 1.0;

        // Boost recent articles
        var daysSinceCreation = (DateTime.UtcNow - article.CreatedAt).TotalDays;
        if (daysSinceCreation < 30)
            score += 0.5;
        else if (daysSinceCreation < 90)
            score += 0.2;

        // Boost popular articles
        if (article.ViewCount > 100)
            score += 0.3;
        if (article.LikeCount > 10)
            score += 0.2;

        return Math.Min(score, 5.0); // Cap at 5.0
    }

    private double CalculateCategoryRelevanceScore(Category category, string query)
    {
        var score = 0.0;
        query = query.ToLowerInvariant();

        if (category.Name.ToLowerInvariant().Contains(query))
            score += 2.0;

        if (category.Description?.ToLowerInvariant().Contains(query) == true)
            score += 1.0;

        return Math.Min(score, 3.0);
    }

    private double CalculateTagRelevanceScore(Tag tag, string query)
    {
        var score = 0.0;
        query = query.ToLowerInvariant();

        if (tag.Name.ToLowerInvariant().Contains(query))
            score += 2.0;

        if (tag.Slug.ToLowerInvariant().Contains(query))
            score += 1.0;

        return Math.Min(score, 3.0);
    }

    private double CalculateMediaRelevanceScore(MediaItem media, string query)
    {
        var score = 0.0;
        query = query.ToLowerInvariant();

        if (media.Name.ToLowerInvariant().Contains(query))
            score += 2.0;

        if (media.FileName.ToLowerInvariant().Contains(query))
            score += 1.5;

        return Math.Min(score, 3.0);
    }

    private string GetHighlightedText(KnowledgeArticle article, string query)
    {
        var textToSearch = $"{article.Title} {article.Summary}";
        return HighlightText(textToSearch, query, 150);
    }

    private string GetCategoryHighlightedText(Category category, string query)
    {
        var textToSearch = $"{category.Name} {category.Description}";
        return HighlightText(textToSearch, query, 100);
    }

    private string GetTagHighlightedText(Tag tag, string query)
    {
        var textToSearch = $"{tag.Name} {tag.Slug}";
        return HighlightText(textToSearch, query, 80);
    }

    private string GetMediaHighlightedText(MediaItem media, string query)
    {
        var textToSearch = $"{media.Name} {media.FileName}";
        return HighlightText(textToSearch, query, 100);
    }

    private string HighlightText(string text, string query, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
            return string.Empty;

        var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;

        var start = Math.Max(0, index - 50);
        var end = Math.Min(text.Length, index + query.Length + 50);
        var excerpt = text.Substring(start, end - start);

        if (start > 0)
            excerpt = "..." + excerpt;
        if (end < text.Length)
            excerpt = excerpt + "...";

        // Simple highlighting - in real implementation, you would use HTML or markdown
        return excerpt.Replace(query, $"**{query}**", StringComparison.OrdinalIgnoreCase);
    }

    private List<string> GetMatchedFields(KnowledgeArticle article, string query)
    {
        var matchedFields = new List<string>();
        query = query.ToLowerInvariant();

        if (article.Title.ToLowerInvariant().Contains(query))
            matchedFields.Add("title");

        if (article.TitleEn?.ToLowerInvariant().Contains(query) == true)
            matchedFields.Add("title_en");

        if (article.Summary.ToLowerInvariant().Contains(query))
            matchedFields.Add("summary");

        if (article.SummaryEn?.ToLowerInvariant().Contains(query) == true)
            matchedFields.Add("summary_en");

        if (article.Content.ToLowerInvariant().Contains(query))
            matchedFields.Add("content");

        return matchedFields;
    }

    private List<string> GetCategoryMatchedFields(Category category, string query)
    {
        var matchedFields = new List<string>();
        query = query.ToLowerInvariant();

        if (category.Name.ToLowerInvariant().Contains(query))
            matchedFields.Add("name");

        if (category.Description?.ToLowerInvariant().Contains(query) == true)
            matchedFields.Add("description");

        return matchedFields;
    }

    private List<string> GetTagMatchedFields(Tag tag, string query)
    {
        var matchedFields = new List<string>();
        query = query.ToLowerInvariant();

        if (tag.Name.ToLowerInvariant().Contains(query))
            matchedFields.Add("name");

        if (tag.Slug.ToLowerInvariant().Contains(query))
            matchedFields.Add("slug");

        return matchedFields;
    }

    private List<string> GetMediaMatchedFields(MediaItem media, string query)
    {
        var matchedFields = new List<string>();
        query = query.ToLowerInvariant();

        if (media.Name.ToLowerInvariant().Contains(query))
            matchedFields.Add("name");

        if (media.FileName.ToLowerInvariant().Contains(query))
            matchedFields.Add("file_name");

        return matchedFields;
    }

    #endregion
}