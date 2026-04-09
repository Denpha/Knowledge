using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewRequestedAt",
                table: "KnowledgeArticles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewRequestedAt",
                table: "KnowledgeArticles");
        }
    }
}
