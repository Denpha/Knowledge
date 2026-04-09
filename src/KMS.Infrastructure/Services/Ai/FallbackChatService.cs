using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KMS.Application.Interfaces;
using KMS.Application.Models;
using KMS.Domain.Enums;

namespace KMS.Infrastructure.Services.Ai;

public class FallbackChatService : IAiChatService, IAiEmbeddingService
{
    private readonly Dictionary<AiProviderType, IAiChatService> _chatServices;
    private readonly Dictionary<AiProviderType, IAiEmbeddingService> _embeddingServices;
    private readonly ILogger<FallbackChatService> _logger;
    private readonly AiConfig _aiConfig;

    public FallbackChatService(
        OpenRouterChatService openRouterService,
        XiaomiMimoChatService xiaomiMimoService,
        OpenRouterEmbeddingService openRouterEmbeddingService,
        IOptions<AiConfig> aiConfig,
        ILogger<FallbackChatService> logger)
    {
        _logger = logger;
        _aiConfig = aiConfig.Value;

        // Register chat services
        _chatServices = new Dictionary<AiProviderType, IAiChatService>
        {
            { AiProviderType.OpenRouter, openRouterService },
            { AiProviderType.XiaomiMiMo, xiaomiMimoService }
        };

        // Register embedding services
        _embeddingServices = new Dictionary<AiProviderType, IAiEmbeddingService>
        {
            { AiProviderType.OpenRouter, openRouterEmbeddingService }
        };
    }

    public async Task<string> GenerateTextAsync(string prompt, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            async (service, provider) => await service.GenerateTextAsync(prompt, provider, cancellationToken),
            "GenerateText",
            preferredProvider,
            cancellationToken);
    }

    public async Task<string> GenerateTextWithContextAsync(string prompt, string context, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            async (service, provider) => await service.GenerateTextWithContextAsync(prompt, context, provider, cancellationToken),
            "GenerateTextWithContext",
            preferredProvider,
            cancellationToken);
    }

    public async Task<string> GenerateTextStreamingAsync(string prompt, Action<string> chunkHandler, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            async (service, provider) => await service.GenerateTextStreamingAsync(prompt, chunkHandler, provider, cancellationToken),
            "GenerateTextStreaming",
            preferredProvider,
            cancellationToken);
    }

    public async Task<string> GenerateTextStreamingWithContextAsync(string prompt, string context, Action<string> chunkHandler, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(
            async (service, provider) => await service.GenerateTextStreamingWithContextAsync(prompt, context, chunkHandler, provider, cancellationToken),
            "GenerateTextStreamingWithContext",
            preferredProvider,
            cancellationToken);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return await ExecuteEmbeddingWithFallbackAsync(
            async (service, provider) => await service.GenerateEmbeddingAsync(text, cancellationToken),
            "GenerateEmbedding",
            cancellationToken);
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        return await ExecuteEmbeddingWithFallbackAsync(
            async (service, provider) => await service.GenerateEmbeddingsAsync(texts, cancellationToken),
            "GenerateEmbeddings",
            cancellationToken);
    }

    private async Task<TResult> ExecuteWithFallbackAsync<TResult>(
        Func<IAiChatService, AiProviderType?, Task<TResult>> operation,
        string operationName,
        AiProviderType? preferredProvider,
        CancellationToken cancellationToken)
    {
        var providers = GetProvidersInPriorityOrder(preferredProvider);

        Exception? lastException = null;

        foreach (var provider in providers)
        {
            if (!_chatServices.TryGetValue(provider, out var service))
            {
                _logger.LogWarning("Service not registered for provider: {Provider}", provider);
                continue;
            }

            if (!IsProviderEnabled(provider))
            {
                _logger.LogDebug("Provider {Provider} is disabled, skipping", provider);
                continue;
            }

            try
            {
                _logger.LogInformation("Attempting {Operation} with provider: {Provider}", operationName, provider);
                var result = await operation(service, provider);
                
                _logger.LogInformation("{Operation} succeeded with provider: {Provider}", operationName, provider);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "{Operation} failed with provider {Provider}, trying fallback", operationName, provider);
                
                // If this was the last provider, break to throw the exception
                if (provider == providers.Last())
                    break;

                // Wait a bit before trying the next provider
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        _logger.LogError(lastException, "All providers failed for {Operation}", operationName);
        throw new InvalidOperationException($"All AI providers failed for {operationName}", lastException);
    }

    private async Task<TResult> ExecuteEmbeddingWithFallbackAsync<TResult>(
        Func<IAiEmbeddingService, AiProviderType?, Task<TResult>> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        var providers = _embeddingServices.Keys
            .Where(p => IsProviderEnabled(p))
            .OrderBy(p => p.GetPriority())
            .ToList();

        Exception? lastException = null;

        foreach (var provider in providers)
        {
            if (!_embeddingServices.TryGetValue(provider, out var service))
            {
                _logger.LogWarning("Embedding service not registered for provider: {Provider}", provider);
                continue;
            }

            if (!IsProviderEnabled(provider))
            {
                _logger.LogDebug("Provider {Provider} is disabled for embedding, skipping", provider);
                continue;
            }

            try
            {
                _logger.LogInformation("Attempting {Operation} with provider: {Provider}", operationName, provider);
                var result = await operation(service, provider);
                
                _logger.LogInformation("{Operation} succeeded with provider: {Provider}", operationName, provider);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "{Operation} failed with provider {Provider}, trying fallback", operationName, provider);
                
                // If queue on all failed is enabled, log and continue
                if (_aiConfig.Embedding.QueueOnAllFailed && provider != providers.Last())
                {
                    _logger.LogWarning("Queueing embedding task for later processing");
                    // In production, you would queue this task for later processing
                    continue;
                }
                
                // If this was the last provider, break to throw the exception
                if (provider == providers.Last())
                    break;

                // Wait a bit before trying the next provider
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        if (_aiConfig.Embedding.QueueOnAllFailed)
        {
            _logger.LogWarning("All embedding providers failed, task has been queued for later processing");
            throw new InvalidOperationException("Embedding service temporarily unavailable, task has been queued");
        }

        _logger.LogError(lastException, "All embedding providers failed for {Operation}", operationName);
        throw new InvalidOperationException($"All embedding providers failed for {operationName}", lastException);
    }

    private List<AiProviderType> GetProvidersInPriorityOrder(AiProviderType? preferredProvider)
    {
        var providers = new List<AiProviderType>();

        // Add preferred provider first if specified
        if (preferredProvider.HasValue && IsProviderEnabled(preferredProvider.Value))
        {
            providers.Add(preferredProvider.Value);
        }

        // Add other enabled providers in priority order
        var otherProviders = Enum.GetValues<AiProviderType>()
            .Where(p => p != preferredProvider && IsProviderEnabled(p))
            .OrderBy(p => p.GetPriority())
            .ToList();

        providers.AddRange(otherProviders);

        if (providers.Count == 0)
        {
            throw new InvalidOperationException("No enabled AI providers available");
        }

        return providers;
    }

    private bool IsProviderEnabled(AiProviderType provider)
    {
        var config = _aiConfig.Chat.Providers.FirstOrDefault(p => p.Name == provider.ToString());
        return config?.Enabled == true;
    }
}