using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KMS.Application.Interfaces;
using KMS.Application.Models;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Entities.Logging;
using KMS.Domain.Enums;
using KMS.Domain.Interfaces;

namespace KMS.Application.Services.Ai;

public class AiWritingService : IAiWritingService
{
    private readonly IAiChatService _aiChatService;
    private readonly IAiEmbeddingService _aiEmbeddingService;
    private readonly IRepository<KnowledgeArticle> _articleRepository;
    private readonly IRepository<AiWritingLog> _aiLogRepository;
    private readonly ILogger<AiWritingService> _logger;
    private readonly AiConfig _config;
    
    public AiWritingService(
        IAiChatService aiChatService,
        IAiEmbeddingService aiEmbeddingService,
        IRepository<KnowledgeArticle> articleRepository,
        IRepository<AiWritingLog> aiLogRepository,
        IOptions<AiConfig> config,
        ILogger<AiWritingService> logger)
    {
        _aiChatService = aiChatService;
        _aiEmbeddingService = aiEmbeddingService;
        _articleRepository = articleRepository;
        _aiLogRepository = aiLogRepository;
        _logger = logger;
        _config = config.Value;
    }
    
    public async Task<string> GenerateDraftAsync(string topic, string? language = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating draft for topic: {Topic}", topic);
        
        // Search for relevant articles in knowledge base
        var relevantArticles = await SearchRelevantArticlesAsync(topic, 5, cancellationToken);
        
        // Build context from relevant articles
        var context = BuildContextFromArticles(relevantArticles);
        
        // Create prompt for draft generation
        var languagePrompt = language == "th" ? "ภาษาไทย" : "English";
        var prompt = $@"Generate a comprehensive knowledge article draft about '{topic}' in {languagePrompt}.

Relevant context from existing knowledge base:
{context}

Please generate a well-structured article with:
1. Clear title (in {languagePrompt})
2. Introduction
3. Main content with sections
4. Conclusion
5. Key takeaways

Write in a professional, informative tone suitable for an academic knowledge management system.";

        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await _aiChatService.GenerateTextWithContextAsync(prompt, context, AiProviderType.OpenRouter, cancellationToken);
            
            // Log the AI usage
            await LogAiUsageAsync(
                userId,
                null,
                "GenerateDraft",
                null,
                prompt,
                response,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                true,
                cancellationToken);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate draft for topic: {Topic}", topic);
            
            await LogAiUsageAsync(
                userId,
                null,
                "GenerateDraft",
                null,
                prompt,
                null,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                false,
                cancellationToken);
            
            throw;
        }
    }
    
    public async Task<string> ImproveTextAsync(string text, ImprovementType improvementType, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Improving text with type: {ImprovementType}", improvementType);
        
        var improvementPrompt = improvementType switch
        {
            ImprovementType.Grammar => "Fix grammar, spelling, and punctuation errors.",
            ImprovementType.Concise => "Make the text more concise and to the point.",
            ImprovementType.Formal => "Make the text more formal and professional.",
            ImprovementType.Expand => "Expand on the ideas with more details and examples.",
            ImprovementType.Simplify => "Simplify the language for easier understanding.",
            _ => "Improve the text."
        };
        
        var prompt = $@"{improvementPrompt}

Original text:
{text}

Improved version:";

        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await _aiChatService.GenerateTextAsync(prompt, AiProviderType.OpenRouter, cancellationToken);
            
            await LogAiUsageAsync(
                userId,
                null,
                "Improve",
                improvementType.ToString(),
                prompt,
                response,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                true,
                cancellationToken);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to improve text with type: {ImprovementType}", improvementType);
            
            await LogAiUsageAsync(
                userId,
                null,
                "Improve",
                improvementType.ToString(),
                prompt,
                null,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                false,
                cancellationToken);
            
            throw;
        }
    }
    
    public async Task<string> TranslateTextAsync(string text, string targetLanguage = "en", Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Translating text to {TargetLanguage}", targetLanguage);
        
        var languageName = targetLanguage.ToLower() switch
        {
            "en" => "English",
            "th" => "ภาษาไทย",
            _ => targetLanguage
        };
        
        var prompt = $@"Translate the following text to {languageName}. 
Preserve the meaning, tone, and formatting as much as possible.

Original text:
{text}

Translation:";

        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await _aiChatService.GenerateTextAsync(prompt, AiProviderType.OpenRouter, cancellationToken);
            
            await LogAiUsageAsync(
                userId,
                null,
                "Translate",
                null,
                prompt,
                response,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                true,
                cancellationToken);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to translate text to {TargetLanguage}", targetLanguage);
            
            await LogAiUsageAsync(
                userId,
                null,
                "Translate",
                null,
                prompt,
                null,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                false,
                cancellationToken);
            
            throw;
        }
    }
    
    public async Task<List<string>> SuggestTagsAsync(string content, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suggesting tags for content");
        
        var prompt = $@"Analyze the following content and suggest 3-5 relevant tags/keywords.
Return only a comma-separated list of tags, nothing else.

Content:
{content}

Tags:";

        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await _aiChatService.GenerateTextAsync(prompt, AiProviderType.OpenRouter, cancellationToken);
            
            // Parse the comma-separated tags
            var tags = response.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Take(5)
                .ToList();
            
            await LogAiUsageAsync(
                userId,
                null,
                "AutoTag",
                null,
                prompt,
                response,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                true,
                cancellationToken);
            
            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest tags for content");
            
            await LogAiUsageAsync(
                userId,
                null,
                "AutoTag",
                null,
                prompt,
                null,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                false,
                cancellationToken);
            
            throw;
        }
    }
    
    public async Task<string> AnswerQuestionAsync(string question, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Answering question: {Question}", question);
        
        // Search for relevant articles
        var relevantArticles = await SearchRelevantArticlesAsync(question, 3, cancellationToken);
        
        // Build context from relevant articles
        var context = BuildContextFromArticles(relevantArticles);
        
        var prompt = $@"Answer the following question based on the provided context from our knowledge base.

Question: {question}

Context from knowledge base:
{context}

Answer:";

        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await _aiChatService.GenerateTextWithContextAsync(prompt, context, AiProviderType.OpenRouter, cancellationToken);
            
            await LogAiUsageAsync(
                userId,
                null,
                "QA",
                null,
                prompt,
                response,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                true,
                cancellationToken);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer question: {Question}", question);
            
            await LogAiUsageAsync(
                userId,
                null,
                "QA",
                null,
                prompt,
                null,
                "qwen/qwen3.6-plus:free",
                "OpenRouter",
                null,
                null,
                (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                false,
                cancellationToken);
            
            throw;
        }
    }
    
    private async Task<List<KnowledgeArticle>> SearchRelevantArticlesAsync(string query, int limit, CancellationToken cancellationToken)
    {
        try
        {
            // Generate embedding for the query
            var queryEmbedding = await _aiEmbeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            
            // Convert to pgvector format (assuming Vector type is used)
            // For now, return top articles by text similarity
            var articles = await _articleRepository.GetAllAsync(cancellationToken);
            
            // Simple text-based relevance for now
            // In production, use pgvector similarity search
            return articles
                .Where(a => a.Status == ArticleStatus.Published)
                .OrderByDescending(a => CalculateRelevanceScore(a, query))
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search articles with embeddings, falling back to text search");
            
            // Fallback to text search
            var articles = await _articleRepository.GetAllAsync(cancellationToken);
            return articles
                .Where(a => a.Status == ArticleStatus.Published)
                .OrderByDescending(a => CalculateRelevanceScore(a, query))
                .Take(limit)
                .ToList();
        }
    }
    
    private double CalculateRelevanceScore(KnowledgeArticle article, string query)
    {
        var queryTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var content = $"{article.Title} {article.Summary} {article.Content}".ToLower();
        
        var score = 0.0;
        foreach (var term in queryTerms)
        {
            if (content.Contains(term))
            {
                score += 1.0;
            }
        }
        
        return score;
    }
    
    private string BuildContextFromArticles(List<KnowledgeArticle> articles)
    {
        if (!articles.Any())
            return "No relevant articles found in the knowledge base.";
        
        var contextBuilder = new System.Text.StringBuilder();
        
        foreach (var article in articles)
        {
            contextBuilder.AppendLine($"Article: {article.Title}");
            contextBuilder.AppendLine($"Summary: {article.Summary}");
            contextBuilder.AppendLine($"Content excerpt: {TruncateText(article.Content, 200)}");
            contextBuilder.AppendLine("---");
        }
        
        return contextBuilder.ToString();
    }
    
    private string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength) + "...";
    }
    
    private async Task LogAiUsageAsync(
        Guid? userId,
        Guid? articleId,
        string featureType,
        string? improvementType,
        string prompt,
        string? response,
        string modelUsed,
        string provider,
        int? inputTokens,
        int? outputTokens,
        int durationMs,
        bool? isAccepted,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new AiWritingLog
            {
                UserId = userId ?? Guid.Empty,
                ArticleId = articleId,
                FeatureType = featureType,
                ImprovementType = improvementType,
                Prompt = prompt,
                Response = response,
                ModelUsed = modelUsed,
                Provider = provider,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                DurationMs = durationMs,
                IsAccepted = isAccepted,
                CreatedAt = DateTime.UtcNow
            };
            
            await _aiLogRepository.AddAsync(log, cancellationToken);
            await _aiLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log AI usage");
            // Don't throw - logging failure shouldn't break the main functionality
        }
    }
}