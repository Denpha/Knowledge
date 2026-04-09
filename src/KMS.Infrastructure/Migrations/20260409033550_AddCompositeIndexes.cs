using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_View_ArticleId_ViewedAt",
                table: "Views",
                columns: new[] { "ArticleId", "ViewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticle_AuthorId_Status",
                table: "KnowledgeArticles",
                columns: new[] { "AuthorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticle_CategoryId_Status",
                table: "KnowledgeArticles",
                columns: new[] { "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticle_Published_Feed",
                table: "KnowledgeArticles",
                columns: new[] { "Status", "Visibility", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticle_Status_CreatedAt",
                table: "KnowledgeArticles",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_View_ArticleId_ViewedAt",
                table: "Views");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeArticle_AuthorId_Status",
                table: "KnowledgeArticles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeArticle_CategoryId_Status",
                table: "KnowledgeArticles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeArticle_Published_Feed",
                table: "KnowledgeArticles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeArticle_Status_CreatedAt",
                table: "KnowledgeArticles");
        }
    }
}
