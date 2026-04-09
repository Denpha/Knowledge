namespace KMS.Application.Models;

public class AiProviderConfig
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

public class AiChatConfig
{
    public List<AiProviderConfig> Providers { get; set; } = new List<AiProviderConfig>();
    public int DefaultMaxTokens { get; set; } = 1000;
    public double DefaultTemperature { get; set; } = 0.7;
}

public class AiEmbeddingConfig
{
    public int Dimensions { get; set; } = 1024;
    public bool QueueOnAllFailed { get; set; } = true;
    public List<AiProviderConfig> Providers { get; set; } = new List<AiProviderConfig>();
}

public class AiConfig
{
    public AiChatConfig Chat { get; set; } = new AiChatConfig();
    public AiEmbeddingConfig Embedding { get; set; } = new AiEmbeddingConfig();
}

public class OpenAiCompatibleRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OpenAiMessage> Messages { get; set; } = new List<OpenAiMessage>();
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public bool? Stream { get; set; }
}

public class OpenAiMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class OpenAiResponse
{
    public List<OpenAiChoice> Choices { get; set; } = new List<OpenAiChoice>();
    public OpenAiUsage? Usage { get; set; }
}

public class OpenAiChoice
{
    public OpenAiMessage Message { get; set; } = new OpenAiMessage();
    public string? FinishReason { get; set; }
}

public class OpenAiUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class OpenAiEmbeddingRequest
{
    public string Model { get; set; } = string.Empty;
    public List<string> Input { get; set; } = new List<string>();
}

public class OpenAiEmbeddingResponse
{
    public List<OpenAiEmbeddingData> Data { get; set; } = new List<OpenAiEmbeddingData>();
    public OpenAiUsage? Usage { get; set; }
}

public class OpenAiEmbeddingData
{
    public int Index { get; set; }
    public List<float> Embedding { get; set; } = new List<float>();
}