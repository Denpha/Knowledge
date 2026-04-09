using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KMS.Application.Interfaces;
using KMS.Application.Models;
using KMS.Application.Services.Ai;
using KMS.Domain.Enums;

namespace KMS.Infrastructure.Services.Ai;

public class OpenRouterEmbeddingService : BaseAiService, IEmbeddingService, IAiEmbeddingService
{
    private readonly AiConfig _aiConfig;
    private readonly Queue<string> _failedEmbeddingQueue = new();
    private readonly object _queueLock = new();
    private bool _isProcessingQueue = false;

    public OpenRouterEmbeddingService(
        HttpClient httpClient,
        IOptions<AiConfig> aiConfig,
        ILogger<OpenRouterEmbeddingService> logger)
        : base(httpClient, GetProviderConfig(aiConfig.Value), logger)
    {
        _aiConfig = aiConfig.Value;

        // Add OpenRouter specific headers
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://kms.rmuti.ac.th");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "KMS - Knowledge Management System");
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating embedding with OpenRouter using model: {Model}", _config.Model);

        try
        {
            return await SendEmbeddingRequestAsync(text, cancellationToken);
        }
        catch (Exception ex) when (_aiConfig.Embedding.QueueOnAllFailed)
        {
            _logger.LogWarning(ex, "Failed to generate embedding, queuing for later processing");
            QueueEmbeddingForLater(text);
            throw new InvalidOperationException("Embedding service temporarily unavailable, task has been queued");
        }
    }

    public async Task<float[][]> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        _logger.LogInformation("Generating embeddings for {Count} texts with OpenRouter", textList.Count);

        try
        {
            var embeddings = await SendEmbeddingsRequestAsync(textList, cancellationToken);
            return embeddings.ToArray();
        }
        catch (Exception ex) when (_aiConfig.Embedding.QueueOnAllFailed)
        {
            _logger.LogWarning(ex, "Failed to generate embeddings, queuing {Count} tasks for later processing", textList.Count);
            foreach (var text in textList)
            {
                QueueEmbeddingForLater(text);
            }
            throw new InvalidOperationException("Embedding service temporarily unavailable, tasks have been queued");
        }
    }

    // IAiEmbeddingService implementation
    async Task<List<float[]>> IAiEmbeddingService.GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken)
    {
        var embeddings = await GenerateEmbeddingsAsync(texts, cancellationToken);
        return embeddings.ToList();
    }

    public double CalculateSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
            throw new ArgumentException("Embeddings must have the same dimension");

        // Calculate cosine similarity
        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            magnitude1 += embedding1[i] * embedding1[i];
            magnitude2 += embedding2[i] * embedding2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    public string GetModelName()
    {
        return _config.Model;
    }

    public int GetDimension()
    {
        return _aiConfig.Embedding.Dimensions;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple health check - try to get a small embedding
            var testRequest = CreateEmbeddingRequest("test");
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(testRequest);
            var response = await _httpClient.PostAsync(
                $"{_config.Endpoint}",
                new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json"),
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void QueueEmbeddingForLater(string text)
    {
        lock (_queueLock)
        {
            _failedEmbeddingQueue.Enqueue(text);
            
            // Start processing queue if not already processing
            if (!_isProcessingQueue)
            {
                _ = ProcessEmbeddingQueueAsync();
            }
        }
    }

    private async Task ProcessEmbeddingQueueAsync()
    {
        lock (_queueLock)
        {
            if (_isProcessingQueue)
                return;
            _isProcessingQueue = true;
        }

        try
        {
            while (true)
            {
                string? text;
                lock (_queueLock)
                {
                    if (!_failedEmbeddingQueue.TryDequeue(out text))
                    {
                        // Queue is empty
                        _isProcessingQueue = false;
                        break;
                    }
                }

                if (text != null)
                {
                    try
                    {
                        // Wait before retrying
                        await Task.Delay(TimeSpan.FromMinutes(5));

                        _logger.LogInformation("Retrying queued embedding task");
                        await GenerateEmbeddingAsync(text);
                        _logger.LogInformation("Successfully processed queued embedding task");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process queued embedding task, re-queuing");
                        
                        // Re-queue for later
                        lock (_queueLock)
                        {
                            _failedEmbeddingQueue.Enqueue(text);
                        }
                        
                        // Wait longer before next retry
                        await Task.Delay(TimeSpan.FromMinutes(10));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in embedding queue processor");
            lock (_queueLock)
            {
                _isProcessingQueue = false;
            }
        }
    }

    private static AiProviderConfig GetProviderConfig(AiConfig aiConfig)
    {
        var config = aiConfig.Embedding.Providers
            .FirstOrDefault(p => p.Name == AiProviderType.OpenRouter.ToString())
            ?? throw new InvalidOperationException($"Embedding configuration for {AiProviderType.OpenRouter} not found");

        return config;
    }
}