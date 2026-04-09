using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KMS.Application.Interfaces;
using KMS.Application.Models;
using KMS.Domain.Enums;

namespace KMS.Infrastructure.Services.Ai;

public abstract class BaseOpenAiCompatibleService
{
    protected readonly HttpClient _httpClient;
    protected readonly AiProviderConfig _config;
    protected readonly ILogger _logger;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected BaseOpenAiCompatibleService(
        HttpClient httpClient,
        AiProviderConfig config,
        ILogger logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure HttpClient
        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }

        // Add custom headers if specified
        if (_config.Headers != null)
        {
            foreach (var header in _config.Headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    protected async Task<HttpResponseMessage> SendRequestWithRetryAsync<TRequest>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        Exception? lastException = null;

        for (int retry = 0; retry <= _config.MaxRetries; retry++)
        {
            try
            {
                if (retry > 0)
                {
                    _logger.LogDebug("Retry attempt {Retry}/{MaxRetries} for {Provider} at {Endpoint}", 
                        retry, _config.MaxRetries, _config.Name, endpoint);
                    
                    // Exponential backoff
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retry - 1)), cancellationToken);
                }

                response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                // Log error but retry on server errors (5xx) or rate limits (429)
                var statusCode = (int)response.StatusCode;
                if (statusCode >= 500 || statusCode == 429)
                {
                    _logger.LogWarning("Request to {Provider} failed with status {StatusCode}, will retry", 
                        _config.Name, statusCode);
                    
                    if (statusCode == 429)
                    {
                        // If rate limited, wait longer
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
                else
                {
                    // Client errors (4xx) shouldn't be retried
                    _logger.LogError("Request to {Provider} failed with status {StatusCode}: {Response}",
                        _config.Name, statusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Request to {Provider} failed (attempt {Retry}/{MaxRetries})",
                    _config.Name, retry + 1, _config.MaxRetries + 1);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Request to {Provider} timed out (attempt {Retry}/{MaxRetries})",
                    _config.Name, retry + 1, _config.MaxRetries + 1);
            }
        }

        throw new HttpRequestException($"All retry attempts failed for {_config.Name}", lastException);
    }

    protected async Task<TResponse> SendRequestWithRetryAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await SendRequestWithRetryAsync(endpoint, request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        try
        {
            return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions) 
                ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from {Provider}: {ResponseContent}",
                _config.Name, responseContent);
            throw;
        }
    }

    protected virtual OpenAiCompatibleRequest CreateChatRequest(
        string prompt,
        string? context = null,
        int? maxTokens = null,
        double? temperature = null)
    {
        var messages = new List<OpenAiMessage>();

        if (!string.IsNullOrEmpty(context))
        {
            messages.Add(new OpenAiMessage
            {
                Role = "system",
                Content = $"Context: {context}\n\nBased on the context above, respond to the user's prompt."
            });
        }

        messages.Add(new OpenAiMessage
        {
            Role = "user",
            Content = prompt
        });

        return new OpenAiCompatibleRequest
        {
            Model = _config.Model,
            Messages = messages,
            MaxTokens = maxTokens,
            Temperature = temperature,
            Stream = false
        };
    }

    protected virtual OpenAiEmbeddingRequest CreateEmbeddingRequest(string text)
    {
        return new OpenAiEmbeddingRequest
        {
            Model = _config.Model,
            Input = new List<string> { text }
        };
    }

    protected virtual OpenAiEmbeddingRequest CreateEmbeddingRequest(List<string> texts)
    {
        return new OpenAiEmbeddingRequest
        {
            Model = _config.Model,
            Input = texts
        };
    }
}