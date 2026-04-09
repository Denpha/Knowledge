using HtmlAgilityPack;
using KMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ExportController(IArticleService articleService)
    {
        _articleService = articleService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // GET /api/export/article/{id}/pdf
    [HttpGet("article/{id}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportArticlePdf(Guid id, CancellationToken cancellationToken)
    {
        var article = await _articleService.GetByIdAsync(id, cancellationToken);
        if (article == null) return NotFound("ไม่พบบทความ");

        var plainText = HtmlToPlainText(article.Content);
        var tags = article.Tags.Select(t => t.Name).ToList();
        var publishDate = (article.PublishedAt ?? DateTime.UtcNow).ToString("dd MMMM yyyy");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(2.2f, Unit.Centimetre);
                page.MarginBottom(2.2f, Unit.Centimetre);
                page.MarginHorizontal(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Tahoma").FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("ระบบจัดการความรู้ | KMS")
                        .FontSize(9).FontColor(Colors.Grey.Medium).AlignRight();
                    col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    // Category breadcrumb
                    col.Item().Text(article.CategoryName)
                        .FontSize(9).FontColor(Colors.Blue.Medium).Bold();

                    // Title
                    col.Item().PaddingTop(6).Text(article.Title)
                        .FontSize(22).Bold().FontColor(Colors.Black);

                    // English title if present
                    if (!string.IsNullOrWhiteSpace(article.TitleEn))
                        col.Item().PaddingTop(2).Text(article.TitleEn)
                            .FontSize(13).Italic().FontColor(Colors.Grey.Darken2);

                    // Meta row
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(txt =>
                            {
                                txt.Span("ผู้เขียน: ").Bold().FontSize(9);
                                txt.Span(article.AuthorName).FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            c.Item().Text(txt =>
                            {
                                txt.Span("วันที่เผยแพร่: ").Bold().FontSize(9);
                                txt.Span(publishDate).FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            c.Item().Text(txt =>
                            {
                                txt.Span("ยอดเข้าชม: ").Bold().FontSize(9);
                                txt.Span($"{article.ViewCount:N0} ครั้ง").FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(txt =>
                            {
                                txt.Span("หมวดหมู่: ").Bold().FontSize(9);
                                txt.Span(article.CategoryName).FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            if (tags.Any())
                            {
                                c.Item().Text(txt =>
                                {
                                    txt.Span("แท็ก: ").Bold().FontSize(9);
                                    txt.Span(string.Join(", ", tags)).FontSize(9).FontColor(Colors.Grey.Darken2);
                                });
                            }
                        });
                    });

                    // Divider
                    col.Item().PaddingVertical(10).LineHorizontal(1f).LineColor(Colors.Blue.Lighten3);

                    // Summary box
                    if (!string.IsNullOrWhiteSpace(article.Summary))
                    {
                        col.Item().Background(Colors.Blue.Lighten5)
                            .Border(1).BorderColor(Colors.Blue.Lighten3)
                            .Padding(10).Column(sCol =>
                            {
                                sCol.Item().Text("สรุป").Bold().FontSize(10).FontColor(Colors.Blue.Medium);
                                sCol.Item().PaddingTop(4).Text(article.Summary)
                                    .FontSize(10).FontColor(Colors.Grey.Darken3).Italic();
                            });
                        col.Item().PaddingTop(14);
                    }

                    // Main content
                    col.Item().Text("เนื้อหา").Bold().FontSize(12).FontColor(Colors.Grey.Darken3);
                    col.Item().PaddingTop(6).Text(plainText)
                        .FontSize(11).LineHeight(1.6f).FontColor(Colors.Black);
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("หน้า ").FontSize(9).FontColor(Colors.Grey.Medium);
                    txt.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    txt.Span(" / ").FontSize(9).FontColor(Colors.Grey.Medium);
                    txt.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        var safeTitle = string.Concat(article.Title.Take(60)).Replace(" ", "_").Replace("/", "-");
        var fileName = $"KMS_{safeTitle}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script/style nodes
        foreach (var node in doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
            node.Remove();

        // Replace block-level elements with newlines
        foreach (var node in doc.DocumentNode.SelectNodes("//p|//br|//div|//h1|//h2|//h3|//h4|//h5|//h6|//li|//tr") ?? Enumerable.Empty<HtmlNode>())
        {
            if (node.Name == "br")
                node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);
            else
                node.ParentNode.ReplaceChild(doc.CreateTextNode("\n" + node.InnerText + "\n"), node);
        }

        var text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
        // Collapse excessive blank lines
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }
}
