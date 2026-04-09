using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentEnField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentEn",
                table: "KnowledgeArticles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentEn",
                table: "ArticleVersions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentEn",
                table: "KnowledgeArticles");

            migrationBuilder.DropColumn(
                name: "ContentEn",
                table: "ArticleVersions");
        }
    }
}
