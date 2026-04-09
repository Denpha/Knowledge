namespace KMS.Domain.Enums;

/// <summary>
/// AI provider types for v4 cloud-only fallback chain
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// OpenRouter - Primary provider (qwen/qwen3.6-plus:free)
    /// </summary>
    OpenRouter = 1,
    
    /// <summary>
    /// XiaomiMiMo - Last resort fallback (mimo-v2-flash)
    /// </summary>
    XiaomiMiMo = 2
}

public static class AiProviderTypeExtensions
{
    public static string ToDisplayString(this AiProviderType providerType)
    {
        return providerType switch
        {
            AiProviderType.OpenRouter => "OpenRouter",
            AiProviderType.XiaomiMiMo => "XiaomiMiMo",
            _ => providerType.ToString()
        };
    }

    public static string GetDefaultModel(this AiProviderType providerType)
    {
        return providerType switch
        {
            AiProviderType.OpenRouter => "qwen/qwen3.6-plus:free",
            AiProviderType.XiaomiMiMo => "mimo-v2-flash",
            _ => throw new ArgumentOutOfRangeException(nameof(providerType))
        };
    }

    public static int GetPriority(this AiProviderType providerType)
    {
        return providerType switch
        {
            AiProviderType.OpenRouter => 1, // Primary
            AiProviderType.XiaomiMiMo => 2, // Last resort
            _ => 99
        };
    }
}