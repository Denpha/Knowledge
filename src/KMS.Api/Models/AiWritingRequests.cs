namespace KMS.Api.Models;

public class GenerateDraftRequest
{
    public string Topic { get; set; } = string.Empty;
    public string? Language { get; set; } = "th";
}

public class ImproveTextRequest
{
    public string Text { get; set; } = string.Empty;
    // Grammar | Concise | Formal | Expand | Simplify
    public string ImprovementType { get; set; } = "Grammar";
}

public class TranslateTextRequest
{
    public string Text { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = "en";
}

public class SuggestTagsRequest
{
    public string Content { get; set; } = string.Empty;
}

public class AiTextResponse
{
    public string Content { get; set; } = string.Empty;
}

public class AiTagsResponse
{
    public List<string> Tags { get; set; } = new();
}

public class AskQuestionRequest
{
    public string Question { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
    public int MaxContextChars { get; set; } = 5000;
    public double SemanticThreshold { get; set; } = 0.65;
    public string PromptProfile { get; set; } = "default";
}

public class AiRagSource
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public double Score { get; set; }
}

public class AiAnswerResponse
{
    public string Answer { get; set; } = string.Empty;
    public string PromptProfileUsed { get; set; } = "default";
    public string ContextPreview { get; set; } = string.Empty;
    public List<AiRagSource> Sources { get; set; } = new();
}

public class EvaluateRagRequest
{
    public string Question { get; set; } = string.Empty;
    public List<string> ExpectedKeywords { get; set; } = new();
    public int TopK { get; set; } = 5;
    public int MaxContextChars { get; set; } = 5000;
    public double SemanticThreshold { get; set; } = 0.65;
    public string PromptProfile { get; set; } = "default";
}

public class RagEvaluationResponse
{
    public string Answer { get; set; } = string.Empty;
    public string PromptProfileUsed { get; set; } = "default";
    public int RetrievedSourceCount { get; set; }
    public int ExpectedKeywordCount { get; set; }
    public int AnswerKeywordHitCount { get; set; }
    public int ContextKeywordHitCount { get; set; }
    public double AnswerKeywordCoverage { get; set; }
    public double ContextKeywordCoverage { get; set; }
    public List<string> MissingKeywordsInAnswer { get; set; } = new();
    public List<AiRagSource> Sources { get; set; } = new();
}

public class EvaluateRagBatchRequest
{
    public List<EvaluateRagCaseRequest> Cases { get; set; } = new();
    public int TopK { get; set; } = 5;
    public int MaxContextChars { get; set; } = 5000;
    public double SemanticThreshold { get; set; } = 0.65;
    public string PromptProfile { get; set; } = "default";
}

public class EvaluateRagCaseRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public List<string> ExpectedKeywords { get; set; } = new();
}

public class RagBenchmarkSummaryResponse
{
    public string PromptProfileUsed { get; set; } = "default";
    public int TotalCases { get; set; }
    public int PassedCases { get; set; }
    public double PassRate { get; set; }
    public double AverageAnswerCoverage { get; set; }
    public double AverageContextCoverage { get; set; }
    public List<RagBenchmarkCaseResult> CaseResults { get; set; } = new();
}

public class EvaluateRagCompareProfilesRequest
{
    public List<EvaluateRagCaseRequest> Cases { get; set; } = new();
    public List<string> Profiles { get; set; } = new();
    public int TopK { get; set; } = 5;
    public int MaxContextChars { get; set; } = 5000;
    public double SemanticThreshold { get; set; } = 0.65;
}

public class RagProfileComparisonResponse
{
    public int TotalCases { get; set; }
    public List<RagBenchmarkSummaryResponse> Profiles { get; set; } = new();
}

public class RagBenchmarkCompareInputSnapshot
{
    public List<EvaluateRagCaseRequest> Cases { get; set; } = new();
    public List<string> Profiles { get; set; } = new();
    public int TopK { get; set; } = 5;
    public int MaxContextChars { get; set; } = 5000;
    public double SemanticThreshold { get; set; } = 0.65;
}

public class RagBenchmarkHistoryItemResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public int TotalCases { get; set; }
    public string? BestProfile { get; set; }
    public double? BestPassRate { get; set; }
    public List<string> Profiles { get; set; } = new();
    public RagBenchmarkCompareInputSnapshot? Input { get; set; }
    public RagProfileComparisonResponse Payload { get; set; } = new();
}

public class RagProfileStabilityMetricResponse
{
    public string Profile { get; set; } = string.Empty;
    public int SampleCount { get; set; }
    public double AveragePassRate { get; set; }
    public double PassRateStdDev { get; set; }
    public double StabilityScore { get; set; }
    public double RecentAveragePassRate { get; set; }
    public double BaselineAveragePassRate { get; set; }
    public double Drift { get; set; }
    public bool DriftFlag { get; set; }
}

public class RagBenchmarkHistoryAnalyticsResponse
{
    public int TotalRuns { get; set; }
    public int WindowSizeRecent { get; set; }
    public int WindowSizeBaseline { get; set; }
    public DateTime? LatestRunAtUtc { get; set; }
    public List<RagProfileStabilityMetricResponse> Profiles { get; set; } = new();
}

public class RagBenchmarkCaseResult
{
    public string CaseId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public int ExpectedKeywordCount { get; set; }
    public int AnswerKeywordHitCount { get; set; }
    public int ContextKeywordHitCount { get; set; }
    public double AnswerKeywordCoverage { get; set; }
    public double ContextKeywordCoverage { get; set; }
    public bool Passed { get; set; }
    public List<string> MissingKeywordsInAnswer { get; set; } = new();
}
