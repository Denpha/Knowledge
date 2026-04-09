using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KMS.Application.Interfaces;
using KMS.Application.Models;
using KMS.Domain.Enums;

namespace KMS.Infrastructure.Services.Ai;

public class XiaomiMimoChatService : BaseOpenAiCompatibleService, IAiChatService
{
    private readonly AiConfig _aiConfig;

    public XiaomiMimoChatService(
        HttpClient httpClient,
        IOptions<AiConfig> aiConfig,
        ILogger<XiaomiMimoChatService> logger)
        : base(httpClient, GetProviderConfig(aiConfig.Value, AiProviderType.XiaomiMiMo), logger)
    {
        _aiConfig = aiConfig.Value;
    }

    public async Task<string> GenerateTextAsync(string prompt, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating text with XiaomiMiMo using model: {Model}", _config.Model);

        var request = CreateChatRequest(prompt, maxTokens: _aiConfig.Chat.DefaultMaxTokens, temperature: _aiConfig.Chat.DefaultTemperature);
        var response = await SendRequestWithRetryAsync<OpenAiCompatibleRequest, OpenAiResponse>(
            $"{_config.Endpoint}/chat/completions",
            request,
            cancellationToken);

        return response.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
    }

    public async Task<string> GenerateTextWithContextAsync(string prompt, string context, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating text with context using XiaomiMiMo");

        var request = CreateChatRequest(prompt, context, maxTokens: _aiConfig.Chat.DefaultMaxTokens, temperature: _aiConfig.Chat.DefaultTemperature);
        var response = await SendRequestWithRetryAsync<OpenAiCompatibleRequest, OpenAiResponse>(
            $"{_config.Endpoint}/chat/completions",
            request,
            cancellationToken);

        return response.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;
    }

    public async Task<string> GenerateTextStreamingAsync(string prompt, Action<string> chunkHandler, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating streaming text with XiaomiMiMo");

        // For streaming, we need to handle SSE
        var request = CreateChatRequest(prompt, maxTokens: _aiConfig.Chat.DefaultMaxTokens, temperature: _aiConfig.Chat.DefaultTemperature);
        request.Stream = true;

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_config.Endpoint}/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        var fullResponse = new System.Text.StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);
            if (data == "[DONE]")
                break;

            try
            {
                var streamResponse = System.Text.Json.JsonSerializer.Deserialize<OpenAiStreamResponse>(data, _jsonOptions);
                if (streamResponse?.Choices?.FirstOrDefault()?.Delta?.Content != null)
                {
                    var chunk = streamResponse.Choices.First().Delta.Content;
                    chunkHandler(chunk);
                    fullResponse.Append(chunk);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Ignore malformed JSON in streaming
            }
        }

        return fullResponse.ToString();
    }

    public async Task<string> GenerateTextStreamingWithContextAsync(string prompt, string context, Action<string> chunkHandler, AiProviderType? preferredProvider = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating streaming text with context using XiaomiMiMo");

        var request = CreateChatRequest(prompt, context, maxTokens: _aiConfig.Chat.DefaultMaxTokens, temperature: _aiConfig.Chat.DefaultTemperature);
        request.Stream = true;

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_config.Endpoint}/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        var fullResponse2 = new System.Text.StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);
            if (data == "[DONE]")
                break;

            try
            {
                var streamResponse = System.Text.Json.JsonSerializer.Deserialize<OpenAiStreamResponse>(data, _jsonOptions);
                if (streamResponse?.Choices?.FirstOrDefault()?.Delta?.Content != null)
                {
                    var chunk = streamResponse.Choices.First().Delta.Content;
                    chunkHandler(chunk);
                    fullResponse2.Append(chunk);
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Ignore malformed JSON in streaming
            }
        }

        return fullResponse2.ToString();
    }

    private static AiProviderConfig GetProviderConfig(AiConfig aiConfig, AiProviderType providerType)
    {
        var config = aiConfig.Chat.Providers
            .FirstOrDefault(p => p.Name == providerType.ToString())
            ?? throw new InvalidOperationException($"Configuration for {providerType} not found");

        return config;
    }

    // Streaming response classes
    private class OpenAiStreamResponse
    {
        public List<OpenAiStreamChoice> Choices { get; set; } = new List<OpenAiStreamChoice>();
    }

    private class OpenAiStreamChoice
    {
        public OpenAiStreamDelta Delta { get; set; } = new OpenAiStreamDelta();
    }

    private class OpenAiStreamDelta
    {
        public string Content { get; set; } = string.Empty;
    }
}