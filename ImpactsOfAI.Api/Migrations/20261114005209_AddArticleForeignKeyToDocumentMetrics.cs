using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactsOfAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleForeignKeyToDocumentMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "DocumentMetrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMetrics_ArticleId",
                table: "DocumentMetrics",
                column: "ArticleId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentMetrics_Articles_ArticleId",
                table: "DocumentMetrics",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentMetrics_Articles_ArticleId",
                table: "DocumentMetrics");

            migrationBuilder.DropIndex(
                name: "IX_DocumentMetrics_ArticleId",
                table: "DocumentMetrics");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "DocumentMetrics");
        }
    }
}
