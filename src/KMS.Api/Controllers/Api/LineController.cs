using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using KMS.Application.DTOs;
using KMS.Application.Interfaces;
using KMS.Application.Models;
using KMS.Domain.Entities;
using KMS.Domain.Enums;
using KMS.Domain.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("line")]
public class LineController : ControllerBase
{
    private const string DedupKeyPrefix = "lineoa.dedup.";
    private static readonly ConcurrentDictionary<string, DateTime> ProcessedEventIds = new();
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> SourceMessageWindows = new();
    private static readonly ConcurrentQueue<DateTime> DuplicateEvents = new();
    private static readonly ConcurrentQueue<DateTime> RateLimitedEvents = new();
    private static readonly ConcurrentQueue<DateTime> ReplyFailures = new();
    private static readonly ConcurrentQueue<DateTime> ProcessingErrors = new();
    private static readonly ConcurrentQueue<DateTime> ProcessedEvents = new();
    private static readonly ConcurrentDictionary<string, DateTime> LastExternalAlertAt = new();
    private static int _dedupCleanupTick;

    private readonly IAiWritingService _aiWritingService;
    private readonly ISearchService _searchService;
    private readonly IAlertChannelService _alertChannelService;
    private readonly IRepository<SystemSetting> _systemSettingRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LineOaConfig _lineConfig;
    private readonly ILogger<LineController> _logger;

    public LineController(
        IAiWritingService aiWritingService,
        ISearchService searchService,
        IAlertChannelService alertChannelService,
        IRepository<SystemSetting> systemSettingRepository,
        IHttpClientFactory httpClientFactory,
        IOptions<LineOaConfig> lineConfig,
        ILogger<LineController> logger)
    {
        _aiWritingService = aiWritingService;
        _searchService = searchService;
        _alertChannelService = alertChannelService;
        _systemSettingRepository = systemSettingRepository;
        _httpClientFactory = httpClientFactory;
        _lineConfig = lineConfig.Value;
        _logger = logger;
    }

    [HttpGet("webhook")]
    [AllowAnonymous]
    public ActionResult<object> Health()
    {
        var telemetryWindow = TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600));
        var snapshot = BuildTelemetrySnapshot(telemetryWindow);

        return Ok(new
        {
            enabled = _lineConfig.Enabled,
            hasSecret = !string.IsNullOrWhiteSpace(_lineConfig.ChannelSecret),
            hasAccessToken = !string.IsNullOrWhiteSpace(_lineConfig.ChannelAccessToken),
            telemetry = snapshot
        });
    }

    [HttpGet("webhook/telemetry")]
    [Authorize(Roles = "Admin")]
    public ActionResult<object> Telemetry()
    {
        var telemetryWindow = TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600));
        return Ok(BuildTelemetrySnapshot(telemetryWindow));
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        if (!_lineConfig.Enabled)
        {
            return Ok();
        }

        if (string.IsNullOrWhiteSpace(_lineConfig.ChannelSecret) || string.IsNullOrWhiteSpace(_lineConfig.ChannelAccessToken))
        {
            _logger.LogWarning("Line OA webhook called but Line settings are incomplete.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Line OA not configured");
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var requestBody = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers["x-line-signature"].ToString();
        if (!IsValidSignature(requestBody, signature, _lineConfig.ChannelSecret))
        {
            _logger.LogWarning("Invalid LINE signature.");
            return Unauthorized();
        }

        try
        {
            using var json = JsonDocument.Parse(requestBody);
            if (!json.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            {
                return Ok();
            }

            foreach (var evt in events.EnumerateArray())
            {
                if (await IsDuplicateEventAsync(evt, cancellationToken))
                {
                    continue;
                }

                await ProcessEventAsync(evt, cancellationToken);
                RecordAndTrim(ProcessedEvents, DateTime.UtcNow, TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600)));
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process LINE webhook payload.");
            await MaybeNotifyAlertAsync("processing-errors", RecordAndTrim(ProcessingErrors, DateTime.UtcNow, TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600))), _lineConfig.AlertProcessingErrorsPerWindow, Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600), cancellationToken);
            return Ok();
        }
    }

    private async Task ProcessEventAsync(JsonElement evt, CancellationToken cancellationToken)
    {
        var eventId = evt.TryGetProperty("webhookEventId", out var eventIdElement)
            ? eventIdElement.GetString() ?? string.Empty
            : string.Empty;

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["LineEventId"] = eventId,
            ["LineEventType"] = evt.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? string.Empty : string.Empty
        });

        var eventType = evt.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
        if (!string.Equals(eventType, "message", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!evt.TryGetProperty("replyToken", out var replyTokenElement))
        {
            return;
        }

        var replyToken = replyTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(replyToken))
        {
            return;
        }

        if (!evt.TryGetProperty("message", out var messageElement))
        {
            return;
        }

        var messageType = messageElement.TryGetProperty("type", out var msgTypeEl) ? msgTypeEl.GetString() : null;
        if (!string.Equals(messageType, "text", StringComparison.OrdinalIgnoreCase))
        {
            await ReplyTextAsync(replyToken, "ระบบรองรับข้อความประเภท text เท่านั้นในตอนนี้", cancellationToken);
            return;
        }

        var question = messageElement.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(question))
        {
            await ReplyTextAsync(replyToken, "ไม่พบข้อความคำถาม", cancellationToken);
            return;
        }

        var normalized = question.Trim();

        if (IsRateLimited(evt, out var retryAfterSeconds))
        {
            _logger.LogInformation("LINE source hit inbound rate limit. RetryAfterSeconds={RetryAfterSeconds}", retryAfterSeconds);
            await MaybeNotifyAlertAsync("rate-limited-events", RecordAndTrim(RateLimitedEvents, DateTime.UtcNow, TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600))), _lineConfig.AlertRateLimitedEventsPerWindow, Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600), cancellationToken);
            await ReplyTextAsync(replyToken, $"ส่งข้อความเร็วเกินไป กรุณารอประมาณ {retryAfterSeconds} วินาทีแล้วลองใหม่", cancellationToken);
            return;
        }

        if (_lineConfig.EnableCommandRouting)
        {
            var routed = await TryHandleCommandAsync(replyToken, normalized, cancellationToken);
            if (routed)
            {
                return;
            }
        }

        if (normalized.Length < 3)
        {
            await ReplyTextAsync(replyToken, BuildHelpMessage(), cancellationToken);
            return;
        }

        try
        {
            var answer = await _aiWritingService.AnswerQuestionAsync(normalized, null, cancellationToken);
            if (answer.Length > _lineConfig.MaxReplyLength)
            {
                answer = answer[.._lineConfig.MaxReplyLength] + "...";
            }

            await ReplyTextAsync(replyToken, answer, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate LINE OA reply.");
            await MaybeNotifyAlertAsync("processing-errors", RecordAndTrim(ProcessingErrors, DateTime.UtcNow, TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600))), _lineConfig.AlertProcessingErrorsPerWindow, Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600), cancellationToken);
            await ReplyTextAsync(replyToken, "ขออภัย ระบบไม่สามารถตอบคำถามได้ในขณะนี้ กรุณาลองใหม่อีกครั้ง", cancellationToken);
        }
    }

    private async Task<bool> TryHandleCommandAsync(string replyToken, string message, CancellationToken cancellationToken)
    {
        var lower = message.ToLowerInvariant();

        if (lower is "help" or "/help" or "ช่วยเหลือ")
        {
            await ReplyTextAsync(replyToken, BuildHelpMessage(), cancellationToken);
            return true;
        }

        if (lower.StartsWith("/search ", StringComparison.Ordinal))
        {
            var query = message[8..].Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                await ReplyTextAsync(replyToken, "รูปแบบคำสั่ง: /search <คำค้นหา>", cancellationToken);
                return true;
            }

            var reply = await BuildSearchReplyAsync(query, cancellationToken);
            await ReplyTextAsync(replyToken, reply, cancellationToken);
            return true;
        }

        if (lower is "menu" or "/menu")
        {
            await ReplyTextAsync(replyToken, BuildHelpMessage(), cancellationToken);
            return true;
        }

        return false;
    }

    private async Task<string> BuildSearchReplyAsync(string query, CancellationToken cancellationToken)
    {
        var maxResults = Math.Clamp(_lineConfig.MaxSearchResults, 1, 5);
        var results = await _searchService.HybridSearchAsync(new SearchRequestParams
        {
            Query = query,
            SearchMode = SearchMode.Hybrid,
            ResultTypes = new List<SearchResultType> { SearchResultType.Article },
            Visibility = Visibility.Public,
            PageNumber = 1,
            PageSize = maxResults,
            SearchInTitle = true,
            SearchInContent = true,
            SearchInSummary = true,
            SearchInTags = true,
            SearchInCategory = true,
            LanguagePreference = LanguagePreference.Both
        }, cancellationToken);

        if (!results.Items.Any() || results.Items.All(x => x.Article == null))
        {
            return $"ไม่พบผลลัพธ์สำหรับ \"{query}\"\nลองใช้คำค้นที่สั้นลง หรือใช้คำสั่งถามตรงๆ ได้เลย";
        }

        var lines = new List<string>
        {
            $"ผลการค้นหา: \"{query}\""
        };

        var articleItems = results.Items.Where(x => x.Article != null).Take(maxResults).ToList();
        for (var i = 0; i < articleItems.Count; i++)
        {
            var article = articleItems[i].Article!;
            var link = $"{_lineConfig.WebBaseUrl.TrimEnd('/')}/articles/{article.Id}";
            lines.Add($"{i + 1}. {article.Title}");
            lines.Add($"   {link}");
        }

        lines.Add("\nส่งคำถามต่อได้เลย เช่น: /search คู่มือ OR ถามตรงเป็นธรรมชาติ");
        return string.Join("\n", lines);
    }

    private string BuildHelpMessage()
    {
        return "คำสั่งที่รองรับ:\n"
               + "- /search <คำค้นหา> : ค้นหาบทความ\n"
               + "- /help หรือ menu : แสดงคำสั่ง\n"
               + "หรือพิมพ์คำถามได้ตรงๆ ระบบจะตอบจากฐานความรู้";
    }

    private async Task ReplyTextAsync(string replyToken, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("LineOA");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _lineConfig.ChannelAccessToken);

        var payload = new
        {
            replyToken,
            messages = new[]
            {
                new
                {
                    type = "text",
                    text = message
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var maxRetries = Math.Clamp(_lineConfig.ReplyMaxRetries, 1, 5);
        var baseDelayMs = Math.Clamp(_lineConfig.ReplyRetryBaseDelayMs, 100, 5000);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_lineConfig.ApiBaseUrl.TrimEnd('/')}/message/reply", requestContent, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (attempt > 1)
                {
                    _logger.LogInformation("LINE reply succeeded after retry attempt {Attempt}", attempt);
                }
                return;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("LINE reply failed (attempt {Attempt}/{MaxRetries}): {StatusCode} - {Body}", attempt, maxRetries, response.StatusCode, body);
            await MaybeNotifyAlertAsync("reply-failures", RecordAndTrim(ReplyFailures, DateTime.UtcNow, TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600))), _lineConfig.AlertReplyFailuresPerWindow, Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600), cancellationToken);

            if (attempt == maxRetries)
            {
                return;
            }

            var delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static bool IsValidSignature(string body, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expected = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    private async Task<bool> IsDuplicateEventAsync(JsonElement evt, CancellationToken cancellationToken)
    {
        if (!evt.TryGetProperty("webhookEventId", out var eventIdElement))
        {
            return false;
        }

        var eventId = eventIdElement.GetString();
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var window = TimeSpan.FromMinutes(Math.Clamp(_lineConfig.EventDedupWindowMinutes, 1, 60));
        var dedupKey = $"{DedupKeyPrefix}{eventId}";

        foreach (var pair in ProcessedEventIds.Where(p => now - p.Value > window).ToList())
        {
            ProcessedEventIds.TryRemove(pair.Key, out _);
        }

        var existing = ProcessedEventIds.TryGetValue(eventId, out var seenAt);
        if (existing && now - seenAt <= window)
        {
            _logger.LogDebug("Skipping duplicate LINE webhook event: {EventId}", eventId);
            await MaybeNotifyAlertAsync("duplicate-events", RecordAndTrim(DuplicateEvents, now, TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600))), _lineConfig.AlertDuplicateEventsPerWindow, Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600), cancellationToken);
            return true;
        }

        var persisted = await _systemSettingRepository.Query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == dedupKey, cancellationToken);

        if (persisted?.UpdatedAt is DateTime persistedAt && now - persistedAt <= window)
        {
            ProcessedEventIds[eventId] = persistedAt;
            _logger.LogDebug("Skipping duplicate LINE webhook event from persistent store: {EventId}", eventId);
            await MaybeNotifyAlertAsync("duplicate-events", RecordAndTrim(DuplicateEvents, now, TimeSpan.FromSeconds(Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600))), _lineConfig.AlertDuplicateEventsPerWindow, Math.Clamp(_lineConfig.TelemetryWindowSeconds, 10, 3600), cancellationToken);
            return true;
        }

        var marker = await _systemSettingRepository.Query
            .FirstOrDefaultAsync(x => x.Key == dedupKey, cancellationToken);

        if (marker is null)
        {
            marker = new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = dedupKey,
                Value = now.ToString("O"),
                Description = "LINE webhook dedup marker",
                Group = "LineOA",
                IsEncrypted = false,
                UpdatedAt = now
            };
            await _systemSettingRepository.AddAsync(marker, cancellationToken);
        }
        else
        {
            marker.Value = now.ToString("O");
            marker.UpdatedAt = now;
            _systemSettingRepository.Update(marker);
        }

        await _systemSettingRepository.SaveChangesAsync(cancellationToken);

        ProcessedEventIds[eventId] = now;
        await CleanupOldDedupMarkersAsync(now - window, cancellationToken);
        return false;
    }

    private async Task CleanupOldDedupMarkersAsync(DateTime cutoff, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _dedupCleanupTick) % 50 != 0)
        {
            return;
        }

        var staleMarkers = await _systemSettingRepository.Query
            .Where(x => x.Key.StartsWith(DedupKeyPrefix) && x.UpdatedAt != null && x.UpdatedAt < cutoff)
            .OrderBy(x => x.UpdatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (!staleMarkers.Any())
        {
            return;
        }

        _systemSettingRepository.RemoveRange(staleMarkers);
        await _systemSettingRepository.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Cleaned {Count} stale LINE dedup markers.", staleMarkers.Count);
    }

    private bool IsRateLimited(JsonElement evt, out int retryAfterSeconds)
    {
        retryAfterSeconds = 0;

        var maxMessages = Math.Max(_lineConfig.InboundRateLimitMaxMessages, 0);
        var windowSeconds = Math.Max(_lineConfig.InboundRateLimitWindowSeconds, 0);
        if (maxMessages == 0 || windowSeconds == 0)
        {
            return false;
        }

        var sourceKey = GetSourceKey(evt);
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var window = TimeSpan.FromSeconds(windowSeconds);
        var queue = SourceMessageWindows.GetOrAdd(sourceKey, _ => new ConcurrentQueue<DateTime>());

        queue.Enqueue(now);
        while (queue.TryPeek(out var head) && now - head > window)
        {
            queue.TryDequeue(out _);
        }

        if (queue.Count <= maxMessages)
        {
            return false;
        }

        if (queue.TryPeek(out var oldestInWindow))
        {
            var retryAt = oldestInWindow.Add(window);
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((retryAt - now).TotalSeconds));
        }
        else
        {
            retryAfterSeconds = windowSeconds;
        }

        return true;
    }

    private static string? GetSourceKey(JsonElement evt)
    {
        if (!evt.TryGetProperty("source", out var source) || source.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var sourceType = source.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "unknown";

        if (source.TryGetProperty("userId", out var userIdEl) && !string.IsNullOrWhiteSpace(userIdEl.GetString()))
        {
            return $"{sourceType}:user:{userIdEl.GetString()}";
        }

        if (source.TryGetProperty("groupId", out var groupIdEl) && !string.IsNullOrWhiteSpace(groupIdEl.GetString()))
        {
            return $"{sourceType}:group:{groupIdEl.GetString()}";
        }

        if (source.TryGetProperty("roomId", out var roomIdEl) && !string.IsNullOrWhiteSpace(roomIdEl.GetString()))
        {
            return $"{sourceType}:room:{roomIdEl.GetString()}";
        }

        return null;
    }

    private object BuildTelemetrySnapshot(TimeSpan window)
    {
        var now = DateTime.UtcNow;
        return new
        {
            windowSeconds = (int)window.TotalSeconds,
            processedEvents = CountInWindow(ProcessedEvents, now, window),
            duplicateEvents = CountInWindow(DuplicateEvents, now, window),
            rateLimitedEvents = CountInWindow(RateLimitedEvents, now, window),
            replyFailures = CountInWindow(ReplyFailures, now, window),
            processingErrors = CountInWindow(ProcessingErrors, now, window)
        };
    }

    private static int RecordAndTrim(ConcurrentQueue<DateTime> queue, DateTime now, TimeSpan window)
    {
        queue.Enqueue(now);
        while (queue.TryPeek(out var head) && now - head > window)
        {
            queue.TryDequeue(out _);
        }

        return queue.Count;
    }

    private static int CountInWindow(ConcurrentQueue<DateTime> queue, DateTime now, TimeSpan window)
    {
        while (queue.TryPeek(out var head) && now - head > window)
        {
            queue.TryDequeue(out _);
        }

        return queue.Count;
    }

    private async Task MaybeNotifyAlertAsync(string metricName, int currentCount, int threshold, int windowSeconds, CancellationToken cancellationToken)
    {
        if (threshold <= 0 || currentCount < threshold)
        {
            return;
        }

        _logger.LogWarning(
            "LINE alert threshold reached: {MetricName} count={CurrentCount} threshold={Threshold} windowSeconds={WindowSeconds}",
            metricName,
            currentCount,
            threshold,
            windowSeconds);

        if (!_lineConfig.EnableExternalAlerts || string.IsNullOrWhiteSpace(_lineConfig.ExternalAlertWebhookUrl))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var cooldown = TimeSpan.FromSeconds(Math.Clamp(_lineConfig.ExternalAlertCooldownSeconds, 30, 3600));

        if (LastExternalAlertAt.TryGetValue(metricName, out var lastAt) && now - lastAt < cooldown)
        {
            return;
        }

        LastExternalAlertAt[metricName] = now;
        await _alertChannelService.SendLineWebhookAlertAsync(metricName, currentCount, threshold, windowSeconds, cancellationToken);
    }
}
