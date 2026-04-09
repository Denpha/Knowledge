using KMS.Domain.Enums;

namespace KMS.Application.Interfaces;

public interface IAiChatService
{
    Task<string> GenerateTextAsync(string prompt, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default);
    Task<string> GenerateTextWithContextAsync(string prompt, string context, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default);
    Task<string> GenerateTextStreamingAsync(string prompt, Action<string> chunkHandler, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default);
    Task<string> GenerateTextStreamingWithContextAsync(string prompt, string context, Action<string> chunkHandler, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default);
}

public interface IAiWritingService
{
    // Generate Draft (RAG from knowledge base)
    Task<string> GenerateDraftAsync(string topic, string? language = null, Guid? userId = null, CancellationToken cancellationToken = default);
    
    // Improve Text
    Task<string> ImproveTextAsync(string text, ImprovementType improvementType, Guid? userId = null, CancellationToken cancellationToken = default);
    
    // Auto Translate
    Task<string> TranslateTextAsync(string text, string targetLanguage = "en", Guid? userId = null, CancellationToken cancellationToken = default);
    
    // Auto Tag suggestions
    Task<List<string>> SuggestTagsAsync(string content, Guid? userId = null, CancellationToken cancellationToken = default);
    
    // RAG QA
    Task<string> AnswerQuestionAsync(string question, Guid? userId = null, CancellationToken cancellationToken = default);
}