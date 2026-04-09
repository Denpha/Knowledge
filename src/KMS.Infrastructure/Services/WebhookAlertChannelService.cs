using System.Text;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KMS.Application.Interfaces;
using KMS.Application.Models;

namespace KMS.Infrastructure.Services;

public class WebhookAlertChannelService : IAlertChannelService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LineOaConfig _lineConfig;
    private readonly ILogger<WebhookAlertChannelService> _logger;

    public WebhookAlertChannelService(
        IHttpClientFactory httpClientFactory,
        IOptions<LineOaConfig> lineConfig,
        ILogger<WebhookAlertChannelService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _lineConfig = lineConfig.Value;
        _logger = logger;
    }

    public async Task SendLineWebhookAlertAsync(
        string metricName,
        int currentCount,
        int threshold,
        int windowSeconds,
        CancellationToken cancellationToken = default)
    {
        if (!_lineConfig.EnableExternalAlerts || string.IsNullOrWhiteSpace(_lineConfig.ExternalAlertWebhookUrl))
        {
            return;
        }

        var client = _httpClientFactory.CreateClient("AlertWebhook");

        var payload = new
        {
            source = "KMS.LineOA",
            severity = "warning",
            metric = metricName,
            count = currentCount,
            threshold,
            windowSeconds,
            triggeredAtUtc = DateTime.UtcNow,
            message = $"LINE alert threshold reached: {metricName} count={currentCount} threshold={threshold} windowSeconds={windowSeconds}"
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(_lineConfig.ExternalAlertWebhookUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "External alert webhook failed: {StatusCode} - {Body}",
                    response.StatusCode,
                    body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External alert webhook call failed.");
        }
    }
}
