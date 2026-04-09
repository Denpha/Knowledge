using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KMS.Api.Helpers;
using KMS.Api.Models;
using KMS.Domain.Enums;
using KMS.Infrastructure.Data;

namespace KMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminDashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET api/admin/dashboard
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // ── Stats ──────────────────────────────────────────────────────────────
        var totalArticles   = await _db.KnowledgeArticles.CountAsync(ct);
        var published       = await _db.KnowledgeArticles.CountAsync(a => a.Status == ArticleStatus.Published, ct);
        var drafts          = await _db.KnowledgeArticles.CountAsync(a => a.Status == ArticleStatus.Draft, ct);
        var totalViews      = await _db.Views.CountAsync(ct);
        var totalUsers      = await _db.Users.CountAsync(ct);
        var activeUsers     = await _db.Users.CountAsync(u => u.IsActive, ct);
        var totalCategories = await _db.Categories.CountAsync(ct);
        var totalComments   = await _db.Comments.CountAsync(ct);

        // ── Articles per month (last 6 months) ────────────────────────────────
        var sixMonthsAgo = now.AddMonths(-5);
        var start = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var articlesByMonth = await _db.KnowledgeArticles
            .Where(a => a.CreatedAt >= start)
            .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var months = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-5 + i))
            .Select(d => new ArticlesPerMonthDto
            {
                Month = d.ToString("MMM yyyy"),
                Count = articlesByMonth
                    .FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0
            })
            .ToList();

        // ── Views per month (last 6 months) ───────────────────────────────────
        var viewsByMonth = await _db.Views
            .Where(v => v.ViewedAt >= start)
            .GroupBy(v => new { v.ViewedAt.Year, v.ViewedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(ct);

        var viewsPerMonth = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-5 + i))
            .Select(d => new ArticlesPerMonthDto
            {
                Month = d.ToString("MMM yyyy"),
                Count = viewsByMonth
                    .FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Count ?? 0
            })
            .ToList();

        // ── Category breakdown ────────────────────────────────────────────────
        var categoryBreakdown = await _db.KnowledgeArticles
            .Where(a => a.Status == ArticleStatus.Published)
            .GroupBy(a => a.Category.Name)
            .Select(g => new CategoryBreakdownDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(ct);

        // ── Top articles by view count ─────────────────────────────────────────
        var topArticles = await _db.KnowledgeArticles
            .Where(a => a.Status == ArticleStatus.Published)
            .OrderByDescending(a => a.ViewCount)
            .Take(5)
            .Select(a => new TopArticleDto
            {
                Id        = a.Id,
                Title     = a.Title,
                ViewCount = a.ViewCount,
                LikeCount = a.LikeCount,
                Category  = a.Category.Name,
                PublishedAt = a.PublishedAt
            })
            .ToListAsync(ct);

        // ── Recent articles ───────────────────────────────────────────────────
        var recentArticles = await _db.KnowledgeArticles
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new RecentArticleDto
            {
                Id        = a.Id,
                Title     = a.Title,
                Status    = a.Status.ToString(),
                Author    = a.Author.FullNameTh,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        // ── Article status breakdown ──────────────────────────────────────────
        var statusBreakdown = await _db.KnowledgeArticles
            .GroupBy(a => a.Status)
            .Select(g => new CategoryBreakdownDto
            {
                Name  = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync(ct);

        var dto = new DashboardDto
        {
            Stats = new DashboardStatsDto
            {
                TotalArticles   = totalArticles,
                PublishedArticles = published,
                DraftArticles   = drafts,
                TotalViews      = totalViews,
                TotalUsers      = totalUsers,
                ActiveUsers     = activeUsers,
                TotalCategories = totalCategories,
                TotalComments   = totalComments,
            },
            ArticlesPerMonth  = months,
            ViewsPerMonth     = viewsPerMonth,
            CategoryBreakdown = categoryBreakdown,
            StatusBreakdown   = statusBreakdown,
            TopArticles       = topArticles,
            RecentArticles    = recentArticles,
        };

        return this.Ok(dto, "Dashboard data retrieved successfully.");
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record DashboardDto
{
    public DashboardStatsDto Stats { get; init; } = new();
    public List<ArticlesPerMonthDto> ArticlesPerMonth { get; init; } = [];
    public List<ArticlesPerMonthDto> ViewsPerMonth { get; init; } = [];
    public List<CategoryBreakdownDto> CategoryBreakdown { get; init; } = [];
    public List<CategoryBreakdownDto> StatusBreakdown { get; init; } = [];
    public List<TopArticleDto> TopArticles { get; init; } = [];
    public List<RecentArticleDto> RecentArticles { get; init; } = [];
}

public record DashboardStatsDto
{
    public int TotalArticles { get; init; }
    public int PublishedArticles { get; init; }
    public int DraftArticles { get; init; }
    public int TotalViews { get; init; }
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int TotalCategories { get; init; }
    public int TotalComments { get; init; }
}

public record ArticlesPerMonthDto
{
    public string Month { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record CategoryBreakdownDto
{
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record TopArticleDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ViewCount { get; init; }
    public int LikeCount { get; init; }
    public string Category { get; init; } = string.Empty;
    public DateTime? PublishedAt { get; init; }
}

public record RecentArticleDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
