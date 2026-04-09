using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Application.DTOs;
using KMS.Application.DTOs.Knowledge;
using KMS.Application.Interfaces;
using KMS.Domain.Entities;
using KMS.Domain.Enums;
using KMS.Domain.Interfaces;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ai")]
public class AiController : ControllerBase
{
    private const string BenchmarkHistoryKey = "ai.rag.benchmark.history";
    private const int MaxPersistedBenchmarkHistory = 20;

    private readonly IAiWritingService _aiWritingService;
    private readonly IAiChatService _aiChatService;
    private readonly ISearchService _searchService;
    private readonly IRepository<SystemSetting> _systemSettingRepository;

    private static readonly HashSet<string> QueryStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "is", "are", "a", "an", "of", "to", "for", "with", "and", "or", "in", "on", "at",
        "ที่", "และ", "หรือ", "ของ", "คือ", "การ", "จาก", "ให้", "ได้", "โดย", "ใน"
    };

    private static readonly Dictionary<string, string> PromptProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = "Answer based on the provided context. If evidence is weak, clearly say uncertainty.",
        ["balanced"] = "Prioritize factual accuracy with concise explanation. Include key procedural steps and avoid speculation.",
        ["strict"] = "Use only supported facts from context. If a required fact is missing, explicitly say it is not found in context."
    };

    public AiController(
        IAiWritingService aiWritingService,
        IAiChatService aiChatService,
        ISearchService searchService,
        IRepository<SystemSetting> systemSettingRepository)
    {
        _aiWritingService = aiWritingService;
        _aiChatService = aiChatService;
        _searchService = searchService;
        _systemSettingRepository = systemSettingRepository;
    }

    [HttpPost("draft")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AiTextResponse>>> GenerateDraft(
        [FromBody] GenerateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
                return this.BadRequest<AiTextResponse>("Topic is required.");

            var userId = GetCurrentUserId();
            var content = await _aiWritingService.GenerateDraftAsync(
                request.Topic,
                request.Language,
                userId,
                cancellationToken);

            return this.Ok(new AiTextResponse { Content = content }, "Draft generated successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<AiTextResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("answer")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AiAnswerResponse>>> AnswerQuestion(
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return this.BadRequest<AiAnswerResponse>("Question is required.");
            }

            var topK = Math.Clamp(request.TopK, 1, 10);
            var maxContextChars = Math.Clamp(request.MaxContextChars, 800, 12000);
            var contextBundle = await BuildRagContextBundleAsync(request.Question, topK, maxContextChars, request.SemanticThreshold, cancellationToken);
            var promptProfile = ResolvePromptProfile(request.PromptProfile);
            var composedPrompt = ComposePrompt(promptProfile, request.Question);

            var answer = await _aiChatService.GenerateTextWithContextAsync(
                composedPrompt,
                contextBundle.Context,
                cancellationToken: cancellationToken);

            return this.Ok(new AiAnswerResponse
            {
                Answer = answer,
                PromptProfileUsed = promptProfile,
                ContextPreview = contextBundle.Context.Length > 600
                    ? contextBundle.Context[..600] + "..."
                    : contextBundle.Context,
                Sources = contextBundle.Sources
            }, "RAG answer generated successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<AiAnswerResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("evaluate")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<RagEvaluationResponse>>> EvaluateRag(
        [FromBody] EvaluateRagRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return this.BadRequest<RagEvaluationResponse>("Question is required.");
            }

            var topK = Math.Clamp(request.TopK, 1, 10);
            var maxContextChars = Math.Clamp(request.MaxContextChars, 800, 12000);
            var contextBundle = await BuildRagContextBundleAsync(request.Question, topK, maxContextChars, request.SemanticThreshold, cancellationToken);
            var promptProfile = ResolvePromptProfile(request.PromptProfile);
            var composedPrompt = ComposePrompt(promptProfile, request.Question);

            var answer = await _aiChatService.GenerateTextWithContextAsync(
                composedPrompt,
                contextBundle.Context,
                cancellationToken: cancellationToken);

            var expectedKeywords = request.ExpectedKeywords
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var answerHits = expectedKeywords.Count(k => answer.Contains(k, StringComparison.OrdinalIgnoreCase));
            var contextHits = expectedKeywords.Count(k => contextBundle.Context.Contains(k, StringComparison.OrdinalIgnoreCase));
            var expectedCount = expectedKeywords.Count;

            var missingInAnswer = expectedKeywords
                .Where(k => !answer.Contains(k, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var response = new RagEvaluationResponse
            {
                Answer = answer,
                PromptProfileUsed = promptProfile,
                RetrievedSourceCount = contextBundle.Sources.Count,
                ExpectedKeywordCount = expectedCount,
                AnswerKeywordHitCount = answerHits,
                ContextKeywordHitCount = contextHits,
                AnswerKeywordCoverage = expectedCount == 0 ? 1 : (double)answerHits / expectedCount,
                ContextKeywordCoverage = expectedCount == 0 ? 1 : (double)contextHits / expectedCount,
                MissingKeywordsInAnswer = missingInAnswer,
                Sources = contextBundle.Sources
            };

            return this.Ok(response, "RAG evaluation completed successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<RagEvaluationResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("evaluate-batch")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<RagBenchmarkSummaryResponse>>> EvaluateRagBatch(
        [FromBody] EvaluateRagBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Cases == null || request.Cases.Count == 0)
            {
                return this.BadRequest<RagBenchmarkSummaryResponse>("At least one benchmark case is required.");
            }

            var topK = Math.Clamp(request.TopK, 1, 10);
            var maxContextChars = Math.Clamp(request.MaxContextChars, 800, 12000);
            var promptProfile = ResolvePromptProfile(request.PromptProfile);

            var caseResults = new List<RagBenchmarkCaseResult>();

            foreach (var benchmarkCase in request.Cases)
            {
                if (string.IsNullOrWhiteSpace(benchmarkCase.Question))
                {
                    return this.BadRequest<RagBenchmarkSummaryResponse>("Each benchmark case must include a non-empty question.");
                }

                var contextBundle = await BuildRagContextBundleAsync(
                    benchmarkCase.Question,
                    topK,
                    maxContextChars,
                    request.SemanticThreshold,
                    cancellationToken);

                var composedPrompt = ComposePrompt(promptProfile, benchmarkCase.Question);

                var answer = await _aiChatService.GenerateTextWithContextAsync(
                    composedPrompt,
                    contextBundle.Context,
                    cancellationToken: cancellationToken);

                var expectedKeywords = benchmarkCase.ExpectedKeywords
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var answerHits = expectedKeywords.Count(k => answer.Contains(k, StringComparison.OrdinalIgnoreCase));
                var contextHits = expectedKeywords.Count(k => contextBundle.Context.Contains(k, StringComparison.OrdinalIgnoreCase));
                var expectedCount = expectedKeywords.Count;
                var answerCoverage = expectedCount == 0 ? 1 : (double)answerHits / expectedCount;
                var contextCoverage = expectedCount == 0 ? 1 : (double)contextHits / expectedCount;

                caseResults.Add(new RagBenchmarkCaseResult
                {
                    CaseId = string.IsNullOrWhiteSpace(benchmarkCase.CaseId)
                        ? $"case-{caseResults.Count + 1}"
                        : benchmarkCase.CaseId,
                    Question = benchmarkCase.Question,
                    ExpectedKeywordCount = expectedCount,
                    AnswerKeywordHitCount = answerHits,
                    ContextKeywordHitCount = contextHits,
                    AnswerKeywordCoverage = answerCoverage,
                    ContextKeywordCoverage = contextCoverage,
                    Passed = answerCoverage >= 0.6 && contextCoverage >= 0.7,
                    MissingKeywordsInAnswer = expectedKeywords
                        .Where(k => !answer.Contains(k, StringComparison.OrdinalIgnoreCase))
                        .ToList()
                });
            }

            var totalCases = caseResults.Count;
            var passedCases = caseResults.Count(x => x.Passed);

            var summary = new RagBenchmarkSummaryResponse
            {
                PromptProfileUsed = promptProfile,
                TotalCases = totalCases,
                PassedCases = passedCases,
                PassRate = totalCases == 0 ? 0 : (double)passedCases / totalCases,
                AverageAnswerCoverage = totalCases == 0 ? 0 : caseResults.Average(x => x.AnswerKeywordCoverage),
                AverageContextCoverage = totalCases == 0 ? 0 : caseResults.Average(x => x.ContextKeywordCoverage),
                CaseResults = caseResults
            };

            return this.Ok(summary, "RAG benchmark completed successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<RagBenchmarkSummaryResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("evaluate-compare")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<RagProfileComparisonResponse>>> EvaluateRagCompareProfiles(
        [FromBody] EvaluateRagCompareProfilesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Cases == null || request.Cases.Count == 0)
            {
                return this.BadRequest<RagProfileComparisonResponse>("At least one benchmark case is required.");
            }

            var profiles = request.Profiles
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (profiles.Count == 0)
            {
                profiles = new List<string> { "default", "balanced", "strict" };
            }

            var topK = Math.Clamp(request.TopK, 1, 10);
            var maxContextChars = Math.Clamp(request.MaxContextChars, 800, 12000);
            var semanticThreshold = Math.Clamp(request.SemanticThreshold, 0, 1);
            var normalizedCases = request.Cases
                .Where(x => !string.IsNullOrWhiteSpace(x.Question))
                .Select((x, index) => new EvaluateRagCaseRequest
                {
                    CaseId = string.IsNullOrWhiteSpace(x.CaseId) ? $"case-{index + 1}" : x.CaseId,
                    Question = x.Question.Trim(),
                    ExpectedKeywords = x.ExpectedKeywords
                        .Where(k => !string.IsNullOrWhiteSpace(k))
                        .Select(k => k.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                })
                .ToList();

            if (normalizedCases.Count == 0)
            {
                return this.BadRequest<RagProfileComparisonResponse>("At least one benchmark case must include a non-empty question.");
            }

            var summaries = new List<RagBenchmarkSummaryResponse>();
            foreach (var profile in profiles)
            {
                var batchRequest = new EvaluateRagBatchRequest
                {
                    Cases = normalizedCases,
                    TopK = topK,
                    MaxContextChars = maxContextChars,
                    SemanticThreshold = semanticThreshold,
                    PromptProfile = profile
                };

                var batchResult = await EvaluateRagBatch(batchRequest, cancellationToken);
                if (batchResult.Result is ObjectResult objectResult && objectResult.StatusCode >= 400)
                {
                    return this.BadRequest<RagProfileComparisonResponse>("Profile comparison failed due to invalid benchmark input.");
                }

                if (batchResult.Value?.Data != null)
                {
                    summaries.Add(batchResult.Value.Data);
                }
            }

            var response = new RagProfileComparisonResponse
            {
                TotalCases = normalizedCases.Count,
                Profiles = summaries.OrderByDescending(x => x.PassRate).ThenByDescending(x => x.AverageAnswerCoverage).ToList()
            };

            var inputSnapshot = new RagBenchmarkCompareInputSnapshot
            {
                Cases = normalizedCases,
                Profiles = profiles,
                TopK = topK,
                MaxContextChars = maxContextChars,
                SemanticThreshold = semanticThreshold
            };

            await PersistBenchmarkHistoryAsync(response, inputSnapshot, GetCurrentUserId(), cancellationToken);

            return this.Ok(response, "RAG profile comparison completed successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<RagProfileComparisonResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("benchmark-history")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<RagBenchmarkHistoryItemResponse>>>> GetBenchmarkHistory(CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await ReadBenchmarkHistoryAsync(cancellationToken);
            return this.Ok(items, "RAG benchmark history retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<List<RagBenchmarkHistoryItemResponse>>($"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete("benchmark-history")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> ClearBenchmarkHistory(CancellationToken cancellationToken = default)
    {
        try
        {
            var setting = await _systemSettingRepository.Query
                .FirstOrDefaultAsync(x => x.Key == BenchmarkHistoryKey, cancellationToken);

            if (setting != null)
            {
                _systemSettingRepository.Remove(setting);
                await _systemSettingRepository.SaveChangesAsync(cancellationToken);
            }

            return this.Ok("cleared", "RAG benchmark history cleared successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<string>($"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("benchmark-history/analytics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RagBenchmarkHistoryAnalyticsResponse>>> GetBenchmarkHistoryAnalytics(CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await ReadBenchmarkHistoryAsync(cancellationToken);
            var analytics = BuildHistoryAnalytics(items);
            return this.Ok(analytics, "RAG benchmark history analytics retrieved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<RagBenchmarkHistoryAnalyticsResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("improve")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AiTextResponse>>> ImproveText(
        [FromBody] ImproveTextRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return this.BadRequest<AiTextResponse>("Text is required.");

            if (!Enum.TryParse<ImprovementType>(request.ImprovementType, true, out var improvementType))
                return this.BadRequest<AiTextResponse>("Invalid improvementType. Allowed: Grammar, Concise, Formal, Expand, Simplify.");

            var userId = GetCurrentUserId();
            var content = await _aiWritingService.ImproveTextAsync(
                request.Text,
                improvementType,
                userId,
                cancellationToken);

            return this.Ok(new AiTextResponse { Content = content }, "Text improved successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<AiTextResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("translate")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AiTextResponse>>> TranslateText(
        [FromBody] TranslateTextRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return this.BadRequest<AiTextResponse>("Text is required.");

            var userId = GetCurrentUserId();
            var content = await _aiWritingService.TranslateTextAsync(
                request.Text,
                request.TargetLanguage,
                userId,
                cancellationToken);

            return this.Ok(new AiTextResponse { Content = content }, "Text translated successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<AiTextResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("tags")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AiTagsResponse>>> SuggestTags(
        [FromBody] SuggestTagsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return this.BadRequest<AiTagsResponse>("Content is required.");

            var userId = GetCurrentUserId();
            var tags = await _aiWritingService.SuggestTagsAsync(
                request.Content,
                userId,
                cancellationToken);

            return this.Ok(new AiTagsResponse { Tags = tags }, "Tags suggested successfully.");
        }
        catch (Exception ex)
        {
            return this.InternalServerError<AiTagsResponse>($"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("stream")]
    [Authorize]
    public async Task Stream(
        [FromQuery] string prompt,
        [FromQuery] string? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("prompt is required", cancellationToken);
            return;
        }

        Response.StatusCode = StatusCodes.Status200OK;
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        var channel = Channel.CreateUnbounded<string>();

        var producer = Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(context))
                {
                    await _aiChatService.GenerateTextStreamingWithContextAsync(
                        prompt,
                        context,
                        chunk => channel.Writer.TryWrite(chunk),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await _aiChatService.GenerateTextStreamingAsync(
                        prompt,
                        chunk => channel.Writer.TryWrite(chunk),
                        cancellationToken: cancellationToken);
                }

                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
            }
        }, cancellationToken);

        try
        {
            await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var safeChunk = chunk
                    .Replace("\r", string.Empty)
                    .Replace("\n", "\\n");

                await Response.WriteAsync($"event: chunk\ndata: {safeChunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await producer;

            await Response.WriteAsync("event: done\ndata: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = ex.Message
                .Replace("\r", string.Empty)
                .Replace("\n", " ");
            await Response.WriteAsync($"event: error\ndata: {message}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpGet("stream-rag")]
    [Authorize]
    public async Task StreamRag(
        [FromQuery] string question,
        [FromQuery] int topK = 5,
        [FromQuery] int maxContextChars = 5000,
        [FromQuery] double semanticThreshold = 0.65,
        [FromQuery] string promptProfile = "default",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("question is required", cancellationToken);
            return;
        }

        var contextBundle = await BuildRagContextBundleAsync(
            question,
            Math.Clamp(topK, 1, 10),
            Math.Clamp(maxContextChars, 800, 12000),
            semanticThreshold,
            cancellationToken);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        var channel = Channel.CreateUnbounded<string>();

        var producer = Task.Run(async () =>
        {
            try
            {
                var resolvedPromptProfile = ResolvePromptProfile(promptProfile);
                var composedPrompt = ComposePrompt(resolvedPromptProfile, question);
                await _aiChatService.GenerateTextStreamingWithContextAsync(
                    composedPrompt,
                    contextBundle.Context,
                    chunk => channel.Writer.TryWrite(chunk),
                    cancellationToken: cancellationToken);

                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
            }
        }, cancellationToken);

        try
        {
            var sourcePayload = string.Join(" | ", contextBundle.Sources.Select(s => $"{s.Title} ({s.Score:0.00})"));
            await Response.WriteAsync($"event: sources\ndata: {sourcePayload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var safeChunk = chunk
                    .Replace("\r", string.Empty)
                    .Replace("\n", "\\n");

                await Response.WriteAsync($"event: chunk\ndata: {safeChunk}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await producer;
            await Response.WriteAsync("event: done\ndata: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var message = ex.Message
                .Replace("\r", string.Empty)
                .Replace("\n", " ");
            await Response.WriteAsync($"event: error\ndata: {message}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private async Task<(string Context, List<AiRagSource> Sources)> BuildRagContextBundleAsync(
        string question,
        int topK,
        int maxContextChars,
        double semanticThreshold,
        CancellationToken cancellationToken)
    {
        var normalizedQuestion = question.Trim();
        var queryVariants = BuildQueryVariants(normalizedQuestion);
        var merged = new Dictionary<Guid, SearchResultDto>();

        foreach (var query in queryVariants)
        {
            var hybrid = await _searchService.HybridSearchAsync(new SearchRequestParams
            {
                Query = query,
                SearchMode = SearchMode.Hybrid,
                ResultTypes = new List<SearchResultType> { SearchResultType.Article },
                Visibility = Visibility.Public,
                SemanticThreshold = semanticThreshold,
                PageNumber = 1,
                PageSize = topK * 2,
                SearchInContent = true,
                SearchInSummary = true,
                SearchInTitle = true,
                SearchInCategory = true,
                SearchInTags = true,
                LanguagePreference = LanguagePreference.Both
            }, cancellationToken);

            foreach (var item in hybrid.Items.Where(x => x.Article != null))
            {
                var articleId = item.Article!.Id;
                if (!merged.ContainsKey(articleId))
                {
                    merged[articleId] = item;
                }
                else
                {
                    var existing = merged[articleId];
                    existing.RelevanceScore = Math.Max(existing.RelevanceScore ?? 0, item.RelevanceScore ?? 0);
                    if (string.IsNullOrWhiteSpace(existing.HighlightedText) && !string.IsNullOrWhiteSpace(item.HighlightedText))
                    {
                        existing.HighlightedText = item.HighlightedText;
                    }
                }
            }
        }

        var ranked = merged.Values
            .OrderByDescending(x => (x.RelevanceScore ?? 0) + TitleOverlapBoost(x, normalizedQuestion))
            .Take(topK)
            .ToList();

        var contextParts = new List<string>();
        var sources = new List<AiRagSource>();
        var remaining = maxContextChars;

        foreach (var item in ranked)
        {
            if (item.Article == null)
            {
                continue;
            }

            var snippet = BuildArticleSnippet(item.Article, item.HighlightedText);
            var part = $"Source: {item.Article.Title}\nCategory: {item.Article.CategoryName}\nExcerpt:\n{snippet}\n";

            if (part.Length > remaining)
            {
                if (remaining < 200)
                {
                    break;
                }
                part = part[..remaining];
            }

            contextParts.Add(part);
            remaining -= part.Length;

            sources.Add(new AiRagSource
            {
                ArticleId = item.Article.Id,
                Title = item.Article.Title,
                Slug = item.Article.Slug,
                CategoryName = item.Article.CategoryName,
                Score = Math.Round(item.RelevanceScore ?? 0, 4)
            });
        }

        var assembledContext = contextParts.Count > 0
            ? string.Join("\n---\n", contextParts)
            : "No high-confidence knowledge context found. Answer conservatively and indicate uncertainty when needed.";

        return (assembledContext, sources);
    }

    private static List<string> BuildQueryVariants(string question)
    {
        var variants = new List<string> { question };

        var tokens = question
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim(',', '.', '?', '!', ';', ':', '"', '\'', '(', ')', '[', ']'))
            .Where(t => t.Length >= 3 && !QueryStopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        if (tokens.Count >= 2)
        {
            variants.Add(string.Join(' ', tokens));
        }

        if (tokens.Count >= 4)
        {
            variants.Add(string.Join(' ', tokens.Take(4)));
        }

        return variants
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private static double TitleOverlapBoost(SearchResultDto result, string question)
    {
        if (result.Article == null)
        {
            return 0;
        }

        var title = result.Article.Title.ToLowerInvariant();
        var tokens = question
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 3)
            .Take(8)
            .ToList();

        var overlaps = tokens.Count(token => title.Contains(token, StringComparison.Ordinal));
        return overlaps * 0.03;
    }

    private static string BuildArticleSnippet(ArticleDto article, string highlighted)
    {
        if (!string.IsNullOrWhiteSpace(highlighted))
        {
            return highlighted.Length > 900 ? highlighted[..900] : highlighted;
        }

        var summary = article.Summary ?? string.Empty;
        var content = article.Content ?? string.Empty;
        var baseText = string.Join(" ", new[] { summary, content }.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (baseText.Length <= 900)
        {
            return baseText;
        }

        return baseText[..900];
    }

    private static RagBenchmarkHistoryAnalyticsResponse BuildHistoryAnalytics(List<RagBenchmarkHistoryItemResponse> items)
    {
        var ordered = items
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        var recentWindow = Math.Min(5, ordered.Count);
        var baselineWindow = Math.Min(5, Math.Max(0, ordered.Count - recentWindow));
        var recentSlice = ordered.Take(recentWindow).ToList();
        var baselineSlice = baselineWindow > 0 ? ordered.Skip(recentWindow).Take(baselineWindow).ToList() : new List<RagBenchmarkHistoryItemResponse>();

        var profileNames = ordered
            .SelectMany(x => x.Payload.Profiles.Select(p => p.PromptProfileUsed))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var profileMetrics = new List<RagProfileStabilityMetricResponse>();
        foreach (var profile in profileNames)
        {
            var allRates = ordered
                .Select(x => x.Payload.Profiles.FirstOrDefault(p => p.PromptProfileUsed.Equals(profile, StringComparison.OrdinalIgnoreCase))?.PassRate)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (allRates.Count == 0)
            {
                continue;
            }

            var average = allRates.Average();
            var variance = allRates.Count <= 1
                ? 0
                : allRates.Sum(x => Math.Pow(x - average, 2)) / allRates.Count;
            var stdDev = Math.Sqrt(variance);

            var recentRates = recentSlice
                .Select(x => x.Payload.Profiles.FirstOrDefault(p => p.PromptProfileUsed.Equals(profile, StringComparison.OrdinalIgnoreCase))?.PassRate)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            var baselineRates = baselineSlice
                .Select(x => x.Payload.Profiles.FirstOrDefault(p => p.PromptProfileUsed.Equals(profile, StringComparison.OrdinalIgnoreCase))?.PassRate)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            var recentAverage = recentRates.Count == 0 ? average : recentRates.Average();
            var baselineAverage = baselineRates.Count == 0 ? average : baselineRates.Average();
            var drift = recentAverage - baselineAverage;

            profileMetrics.Add(new RagProfileStabilityMetricResponse
            {
                Profile = profile,
                SampleCount = allRates.Count,
                AveragePassRate = average,
                PassRateStdDev = stdDev,
                StabilityScore = Math.Clamp(1 - stdDev, 0, 1),
                RecentAveragePassRate = recentAverage,
                BaselineAveragePassRate = baselineAverage,
                Drift = drift,
                DriftFlag = baselineRates.Count >= 2 && Math.Abs(drift) >= 0.10
            });
        }

        return new RagBenchmarkHistoryAnalyticsResponse
        {
            TotalRuns = ordered.Count,
            WindowSizeRecent = recentWindow,
            WindowSizeBaseline = baselineWindow,
            LatestRunAtUtc = ordered.FirstOrDefault()?.CreatedAtUtc,
            Profiles = profileMetrics
                .OrderByDescending(x => x.AveragePassRate)
                .ThenByDescending(x => x.StabilityScore)
                .ToList()
        };
    }

    private async Task PersistBenchmarkHistoryAsync(
        RagProfileComparisonResponse payload,
        RagBenchmarkCompareInputSnapshot inputSnapshot,
        Guid? createdByUserId,
        CancellationToken cancellationToken)
    {
        var existing = await ReadBenchmarkHistoryAsync(cancellationToken);
        var best = payload.Profiles.OrderByDescending(x => x.PassRate).ThenByDescending(x => x.AverageAnswerCoverage).FirstOrDefault();

        var entry = new RagBenchmarkHistoryItemResponse
        {
            Id = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..30],
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            TotalCases = payload.TotalCases,
            BestProfile = best?.PromptProfileUsed,
            BestPassRate = best?.PassRate,
            Profiles = payload.Profiles.Select(x => x.PromptProfileUsed).ToList(),
            Input = inputSnapshot,
            Payload = payload
        };

        var next = new List<RagBenchmarkHistoryItemResponse> { entry };
        next.AddRange(existing);
        next = next.Take(MaxPersistedBenchmarkHistory).ToList();

        var setting = await _systemSettingRepository.Query
            .FirstOrDefaultAsync(x => x.Key == BenchmarkHistoryKey, cancellationToken);

        var serialized = JsonSerializer.Serialize(next);

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = BenchmarkHistoryKey,
                Group = "AiRag",
                Description = "Persisted RAG benchmark compare history",
                IsEncrypted = false,
                Value = serialized,
                UpdatedAt = DateTime.UtcNow,
                UpdatedById = createdByUserId
            };

            await _systemSettingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = serialized;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedById = createdByUserId;
            _systemSettingRepository.Update(setting);
        }

        await _systemSettingRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<RagBenchmarkHistoryItemResponse>> ReadBenchmarkHistoryAsync(CancellationToken cancellationToken)
    {
        var setting = await _systemSettingRepository.Query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == BenchmarkHistoryKey, cancellationToken);

        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return new List<RagBenchmarkHistoryItemResponse>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<RagBenchmarkHistoryItemResponse>>(setting.Value);
            return parsed?.OrderByDescending(x => x.CreatedAtUtc).Take(MaxPersistedBenchmarkHistory).ToList()
                   ?? new List<RagBenchmarkHistoryItemResponse>();
        }
        catch
        {
            return new List<RagBenchmarkHistoryItemResponse>();
        }
    }

    private static string ResolvePromptProfile(string? requestedProfile)
    {
        if (string.IsNullOrWhiteSpace(requestedProfile))
        {
            return "default";
        }

        return PromptProfiles.ContainsKey(requestedProfile) ? requestedProfile : "default";
    }

    private static string ComposePrompt(string profile, string question)
    {
        var instruction = PromptProfiles.TryGetValue(profile, out var template)
            ? template
            : PromptProfiles["default"];

        return $"{instruction}\n\nQuestion: {question}";
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
