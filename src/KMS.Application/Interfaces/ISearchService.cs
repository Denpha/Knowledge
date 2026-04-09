using KMS.Application.DTOs;

namespace KMS.Application.Interfaces;

public interface ISearchService
{
    // Full-text search
    Task<PaginatedResult<SearchResultDto>> SearchAsync(SearchRequestParams searchParams, CancellationToken cancellationToken = default);
    
    // Semantic search using pgvector
    Task<List<SearchResultDto>> SemanticSearchAsync(SemanticSearchParams searchParams, CancellationToken cancellationToken = default);
    
    // Hybrid search (combines full-text and semantic)
    Task<PaginatedResult<SearchResultDto>> HybridSearchAsync(SearchRequestParams searchParams, CancellationToken cancellationToken = default);
    
    // Autocomplete/suggestions
    Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default);
    
    // Search statistics
    Task<SearchStatsDto> GetSearchStatsAsync(CancellationToken cancellationToken = default);
    
    // Generate embedding for text (for semantic search)
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    
    // Update embeddings for existing articles
    Task<int> UpdateArticleEmbeddingsAsync(CancellationToken cancellationToken = default);
    
    // Search logging
    Task LogSearchAsync(string query, Guid? userId, SearchMode mode, int resultCount, CancellationToken cancellationToken = default);
}