namespace KMS.Application.Models;

public class LineOaConfig
{
    public bool Enabled { get; set; }
    public string ChannelSecret { get; set; } = string.Empty;
    public string ChannelAccessToken { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.line.me/v2/bot";
    public int MaxReplyLength { get; set; } = 1200;
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
    public bool EnableCommandRouting { get; set; } = true;
    public int MaxSearchResults { get; set; } = 3;
    public int EventDedupWindowMinutes { get; set; } = 10;
    public int ReplyMaxRetries { get; set; } = 3;
    public int ReplyRetryBaseDelayMs { get; set; } = 250;
    public int InboundRateLimitWindowSeconds { get; set; } = 60;
    public int InboundRateLimitMaxMessages { get; set; } = 20;
    public int TelemetryWindowSeconds { get; set; } = 60;
    public int AlertDuplicateEventsPerWindow { get; set; } = 50;
    public int AlertRateLimitedEventsPerWindow { get; set; } = 30;
    public int AlertReplyFailuresPerWindow { get; set; } = 10;
    public int AlertProcessingErrorsPerWindow { get; set; } = 10;
    public bool EnableExternalAlerts { get; set; } = false;
    public string ExternalAlertWebhookUrl { get; set; } = string.Empty;
    public int ExternalAlertCooldownSeconds { get; set; } = 300;
}
