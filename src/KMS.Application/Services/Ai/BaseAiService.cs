using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using KMS.Application.Interfaces;
using KMS.Application.Models;

namespace KMS.Application.Services.Ai;

public abstract class BaseAiService
{
    protected readonly HttpClient _httpClient;
    protected readonly AiProviderConfig _config;
    protected readonly ILogger _logger;
    
    protected BaseAiService(HttpClient httpClient, AiProviderConfig config, ILogger logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Clear();
        
        // Add API key if provided
        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }
        
        // Add custom headers if provided
        if (_config.Headers != null)
        {
            foreach (var header in _config.Headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
    
    protected virtual async Task<string> SendChatRequestAsync(
        string prompt, 
        string? context = null, 
        int? maxTokens = null, 
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<OpenAiMessage>();
            
            // Add system prompt if context is provided
            if (!string.IsNullOrEmpty(context))
            {
                messages.Add(new OpenAiMessage
                {
                    Role = "system",
                    Content = $"Context: {context}\n\nUse the provided context to answer the user's question accurately."
                });
            }
            
            messages.Add(new OpenAiMessage
            {
                Role = "user",
                Content = prompt
            });
            
            var request = new OpenAiCompatibleRequest
            {
                Model = _config.Model,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature,
                Stream = false
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_config.Endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("AI API request failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"AI API request failed: {response.StatusCode} - {errorContent}");
            }
            
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var openAiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseJson);
            
            if (openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                throw new InvalidOperationException("Invalid response from AI API");
            }
            
            return openAiResponse.Choices.First().Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI chat request for provider {Provider}", _config.Name);
            throw;
        }
    }
    
    protected virtual async Task<string> SendChatRequestStreamingAsync(
        string prompt, 
        string? context = null, 
        Action<string>? chunkHandler = null,
        int? maxTokens = null, 
        double? temperature = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new List<OpenAiMessage>();
            
            // Add system prompt if context is provided
            if (!string.IsNullOrEmpty(context))
            {
                messages.Add(new OpenAiMessage
                {
                    Role = "system",
                    Content = $"Context: {context}\n\nUse the provided context to answer the user's question accurately."
                });
            }
            
            messages.Add(new OpenAiMessage
            {
                Role = "user",
                Content = prompt
            });
            
            var request = new OpenAiCompatibleRequest
            {
                Model = _config.Model,
                Messages = messages,
                MaxTokens = maxTokens,
                Temperature = temperature,
                Stream = true
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_config.Endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("AI API streaming request failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"AI API streaming request failed: {response.StatusCode} - {errorContent}");
            }
            
            var fullResponse = new StringBuilder();
            
            using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var reader = new StreamReader(stream))
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null) break;
                    
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                        continue;
                    
                    var data = line.Substring(6); // Remove "data: " prefix
                    
                    if (data == "[DONE]")
                        break;
                    
                    try
                    {
                        var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(data);
                        var choices = jsonDoc?.RootElement.GetProperty("choices");
                        
                        if (choices?.ValueKind == JsonValueKind.Array && choices.Value.GetArrayLength() > 0)
                        {
                            var delta = choices.Value[0].GetProperty("delta");
                            
                            if (delta.TryGetProperty("content", out var contentElement))
                            {
                                var chunk = contentElement.GetString();
                                if (!string.IsNullOrEmpty(chunk))
                                {
                                    fullResponse.Append(chunk);
                                    chunkHandler?.Invoke(chunk);
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse SSE chunk");
                    }
                }
            }
            
            return fullResponse.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI streaming request for provider {Provider}", _config.Name);
            throw;
        }
    }
    
    protected virtual async Task<float[]> SendEmbeddingRequestAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OpenAiEmbeddingRequest
            {
                Model = _config.Model,
                Input = new List<string> { text }
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_config.Endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("AI Embedding API request failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"AI Embedding API request failed: {response.StatusCode} - {errorContent}");
            }
            
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var openAiResponse = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(responseJson);
            
            if (openAiResponse?.Data?.FirstOrDefault()?.Embedding == null)
            {
                throw new InvalidOperationException("Invalid response from AI Embedding API");
            }
            
            return openAiResponse.Data.First().Embedding.Select(e => (float)e).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI embedding request for provider {Provider}", _config.Name);
            throw;
        }
    }
    
    protected virtual async Task<List<float[]>> SendEmbeddingsRequestAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OpenAiEmbeddingRequest
            {
                Model = _config.Model,
                Input = texts
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_config.Endpoint, content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("AI Embedding API batch request failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"AI Embedding API batch request failed: {response.StatusCode} - {errorContent}");
            }
            
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var openAiResponse = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(responseJson);
            
            if (openAiResponse?.Data == null)
            {
                throw new InvalidOperationException("Invalid response from AI Embedding API");
            }
            
            return openAiResponse.Data
                .OrderBy(d => d.Index)
                .Select(d => d.Embedding.Select(e => (float)e).ToArray())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI batch embedding request for provider {Provider}", _config.Name);
            throw;
        }
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