using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Application.DTOs;
using KMS.Application.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    // GET: api/search
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<SearchResultDto>>>> Search(
        [FromQuery] string query,
        [FromQuery] SearchMode searchMode = SearchMode.FullText,
        [FromQuery] List<SearchResultType> resultTypes = null!,
        [FromQuery] List<Guid>? categoryIds = null,
        [FromQuery] List<string>? tags = null,
        [FromQuery] Domain.Enums.ArticleStatus? articleStatus = null,
        [FromQuery] Domain.Enums.Visibility? visibility = null,
        [FromQuery] bool useSemanticSearch = false,
        [FromQuery] double? semanticThreshold = 0.7,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        [FromQuery] DateTime? publishedFrom = null,
        [FromQuery] DateTime? publishedTo = null,
        [FromQuery] bool searchInTitle = true,
        [FromQuery] bool searchInContent = true,
        [FromQuery] bool searchInSummary = true,
        [FromQuery] bool searchInTags = true,
        [FromQuery] bool searchInCategory = true,
        [FromQuery] LanguagePreference languagePreference = LanguagePreference.Primary,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return this.BadRequest<PaginatedResult<SearchResultDto>>("Search query is required.");
            }

            var searchParams = new SearchRequestParams
            {
                Query = query,
                SearchMode = searchMode,
                ResultTypes = resultTypes ?? new List<SearchResultType> { SearchResultType.Article, SearchResultType.Category, SearchResultType.Tag },
                CategoryIds = categoryIds,
                Tags = tags,
                ArticleStatus = articleStatus,
                Visibility = visibility,
                UseSemanticSearch = useSemanticSearch,
                SemanticThreshold = semanticThreshold,
                CreatedFrom = createdFrom,
                CreatedTo = createdTo,
                PublishedFrom = publishedFrom,
                PublishedTo = publishedTo,
                SearchInTitle = searchInTitle,
                SearchInContent = searchInContent,
                SearchInSummary = searchInSummary,
                SearchInTags = searchInTags,
                SearchInCategory = searchInCategory,
                LanguagePreference = languagePreference,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            PaginatedResult<SearchResultDto> results;
            
            switch (searchMode)
            {
                case SearchMode.FullText:
                    results = await _searchService.SearchAsync(searchParams, cancellationToken);
                    break;
                case SearchMode.Semantic:
                    var semanticResults = await _searchService.SemanticSearchAsync(new SemanticSearchParams
                    {
                        Query = query,
                        Limit = pageSize,
                        SimilarityThreshold = semanticThreshold,
                        CategoryIds = categoryIds,
                        ArticleStatus = articleStatus,
                        Visibility = visibility,
                        LanguagePreference = languagePreference
                    }, cancellationToken);
                    
                    results = new PaginatedResult<SearchResultDto>
                    {
                        Items = semanticResults,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = semanticResults.Count
                    };
                    break;
                case SearchMode.Hybrid:
                    results = await _searchService.HybridSearchAsync(searchParams, cancellationToken);
                    break;
                default:
                    results = await _searchService.SearchAsync(searchParams, cancellationToken);
                    break;
            }

            return this.Ok(results, $"Search completed successfully. Found {results.TotalCount} results.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search for query: {Query}", query);
            return this.InternalServerError<PaginatedResult<SearchResultDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/search/suggestions
    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<List<SearchSuggestionDto>>>> GetSuggestions(
        [FromQuery] string query,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return this.BadRequest<List<SearchSuggestionDto>>("Query must be at least 2 characters long.");
            }

            var suggestions = await _searchService.GetSuggestionsAsync(query, limit, cancellationToken);
            return this.Ok(suggestions, "Search suggestions retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            return this.InternalServerError<List<SearchSuggestionDto>>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/search/stats
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<SearchStatsDto>>> GetSearchStats(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _searchService.GetSearchStatsAsync(cancellationToken);
            return this.Ok(stats, "Search statistics retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search statistics");
            return this.InternalServerError<SearchStatsDto>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/search/embeddings/update
    [HttpPost("embeddings/update")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> UpdateEmbeddings(CancellationToken cancellationToken = default)
    {
        try
        {
            var updatedCount = await _searchService.UpdateArticleEmbeddingsAsync(cancellationToken);
            return this.Ok(updatedCount, $"Updated embeddings for {updatedCount} articles.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating article embeddings");
            return this.InternalServerError<int>($"Internal server error: {ex.Message}");
        }
    }

    // POST: api/search/embedding/generate
    [HttpPost("embedding/generate")]
    [Authorize(Policy = "CanWrite")]
    public async Task<ActionResult<ApiResponse<float[]>>> GenerateEmbedding(
        [FromBody] GenerateEmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return this.BadRequest<float[]>("Text is required.");
            }

            var embedding = await _searchService.GenerateEmbeddingAsync(request.Text, cancellationToken);
            return this.Ok(embedding, "Embedding generated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text: {Text}", request.Text);
            return this.InternalServerError<float[]>($"Internal server error: {ex.Message}");
        }
    }

    // GET: api/search/test
    [HttpGet("test")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> TestSearch(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test full-text search
            var searchParams = new SearchRequestParams
            {
                Query = "test",
                PageNumber = 1,
                PageSize = 5
            };

            var results = await _searchService.SearchAsync(searchParams, cancellationToken);
            
            // Test semantic search
            var semanticParams = new SemanticSearchParams
            {
                Query = "test",
                Limit = 3
            };

            var semanticResults = await _searchService.SemanticSearchAsync(semanticParams, cancellationToken);
            
            // Test suggestions
            var suggestions = await _searchService.GetSuggestionsAsync("test", 3, cancellationToken);

            return this.Ok(
                $"Search service is working. Full-text results: {results.TotalCount}, Semantic results: {semanticResults.Count}, Suggestions: {suggestions.Count}",
                "Search service test completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing search service");
            return this.InternalServerError<string>($"Search service test failed: {ex.Message}");
        }
    }
}

public class GenerateEmbeddingRequest
{
    public string Text { get; set; } = string.Empty;
}