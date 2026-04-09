using KMS.Application.DTOs.Knowledge;
using KMS.Application.DTOs.Media;
using KMS.Domain.Enums;

namespace KMS.Application.DTOs;

public class SearchResultDto : BaseDto
{
    public SearchResultType ResultType { get; set; }
    public double? RelevanceScore { get; set; }
    
    // Article result
    public ArticleDto? Article { get; set; }
    
    // Category result
    public CategoryDto? Category { get; set; }
    
    // Tag result
    public TagDto? Tag { get; set; }
    
    // Media result
    public MediaItemDto? MediaItem { get; set; }
    
    public string HighlightedText { get; set; } = string.Empty;
    public List<string> MatchedFields { get; set; } = new List<string>();
}

public class SearchRequestParams : PaginationParams
{
    public string Query { get; set; } = string.Empty;
    public SearchMode SearchMode { get; set; } = SearchMode.FullText;
    public List<SearchResultType> ResultTypes { get; set; } = new List<SearchResultType>();
    
    // Filters
    public List<Guid>? CategoryIds { get; set; }
    public List<string>? Tags { get; set; }
    public Domain.Enums.ArticleStatus? ArticleStatus { get; set; }
    public Domain.Enums.Visibility? Visibility { get; set; }
    
    // Semantic search specific
    public bool UseSemanticSearch { get; set; } = false;
    public double? SemanticThreshold { get; set; } = 0.7;
    
    // Date filters
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? PublishedFrom { get; set; }
    public DateTime? PublishedTo { get; set; }
    
    // Full-text search options
    public bool SearchInTitle { get; set; } = true;
    public bool SearchInContent { get; set; } = true;
    public bool SearchInSummary { get; set; } = true;
    public bool SearchInTags { get; set; } = true;
    public bool SearchInCategory { get; set; } = true;
    
    // Language preference
    public LanguagePreference LanguagePreference { get; set; } = LanguagePreference.Primary;
}

public class SemanticSearchParams
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
    public double? SimilarityThreshold { get; set; } = 0.7;
    
    // Filters
    public List<Guid>? CategoryIds { get; set; }
    public ArticleStatus? ArticleStatus { get; set; }
    public Visibility? Visibility { get; set; }
    
    public LanguagePreference LanguagePreference { get; set; } = LanguagePreference.Primary;
}

public class SearchSuggestionDto
{
    public string Query { get; set; } = string.Empty;
    public SearchResultType ResultType { get; set; }
    public int? Count { get; set; }
}

public class SearchStatsDto
{
    public int TotalArticles { get; set; }
    public int TotalCategories { get; set; }
    public int TotalTags { get; set; }
    public int TotalMediaItems { get; set; }
    
    public Dictionary<string, int> TopCategories { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> TopTags { get; set; } = new Dictionary<string, int>();
    public Dictionary<int, int> MonthlyArticleCounts { get; set; } = new Dictionary<int, int>();
}

public enum SearchMode
{
    FullText,
    Semantic,
    Hybrid
}

public enum SearchResultType
{
    Article,
    Category,
    Tag,
    Media
}

public enum LanguagePreference
{
    Primary,    // Thai
    Secondary,  // English
    Both
}