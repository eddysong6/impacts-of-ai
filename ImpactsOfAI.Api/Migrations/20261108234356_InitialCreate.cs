using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImpactsOfAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    Publisher = table.Column<string>(type: "text", nullable: false),
                    URL = table.Column<string>(type: "text", nullable: false),
                    RigorLevel = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    CompositeScore = table.Column<int>(type: "integer", nullable: false),
                    Volume = table.Column<string>(type: "text", nullable: true),
                    Issue = table.Column<string>(type: "text", nullable: true),
                    Doi = table.Column<string>(type: "text", nullable: true),
                    Journal = table.Column<string>(type: "text", nullable: false),
                    Pages = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    File = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false, comment: "CSV: file"),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "CSV: status"),
                    EngineVersion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "CSV: engine version"),
                    Pages = table.Column<int>(type: "integer", nullable: false, comment: "CSV: pages"),
                    Doi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "CSV: DOI (null = n/a)"),
                    NumWords = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Words"),
                    NumCharWithSpaces = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Char (with spaces)"),
                    NumCharWithoutSpaces = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Char (without spaces)"),
                    NumStatements = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Statements"),
                    NumParagraphs = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Paragraphs"),
                    ParagraphsLeftOutNoTerminalPunctuation = table.Column<int>(type: "integer", nullable: true, comment: "CSV: left out: no terminal punctuation"),
                    ParagraphsLeftOutShortItemLists = table.Column<int>(type: "integer", nullable: true, comment: "CSV: left out: lists of short items"),
                    ParagraphsLeftOutRunInLabels = table.Column<int>(type: "integer", nullable: true, comment: "CSV: left out: run-in labels standing alone"),
                    ParagraphsLeftOutCreditLines = table.Column<int>(type: "integer", nullable: true, comment: "CSV: left out: credit lines and sign-offs"),
                    Headings = table.Column<int>(type: "integer", nullable: true, comment: "CSV: headings"),
                    NumParagraphs3Plus = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Paragraphs_3plus"),
                    Paragraphs3PlusLeftOutTwoOrFewerStatements = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Paragraphs_3plus: left out, two or fewer statements"),
                    NumObjects = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Objects"),
                    Logos = table.Column<int>(type: "integer", nullable: true, comment: "CSV: logos"),
                    Images = table.Column<int>(type: "integer", nullable: true, comment: "CSV: images"),
                    VectorFigures = table.Column<int>(type: "integer", nullable: true, comment: "CSV: vector figures"),
                    UncertainObjects = table.Column<int>(type: "integer", nullable: true, comment: "CSV: uncertain objects"),
                    NumCitations = table.Column<int>(type: "integer", nullable: true, comment: "CSV: Num_Citations"),
                    NumberedSeriesCitations = table.Column<int>(type: "integer", nullable: true, comment: "CSV: numbered series"),
                    AuthorYearCitations = table.Column<int>(type: "integer", nullable: true, comment: "CSV: author–year"),
                    BackReferences = table.Column<int>(type: "integer", nullable: true, comment: "CSV: back-references"),
                    FrontMatterWords = table.Column<int>(type: "integer", nullable: true, comment: "CSV: front-matter words"),
                    BackMatterWords = table.Column<int>(type: "integer", nullable: true, comment: "CSV: back-matter words"),
                    UncertainHyphens = table.Column<int>(type: "integer", nullable: true, comment: "CSV: uncertain hyphens"),
                    FleschReadingEase = table.Column<double>(type: "double precision", nullable: true, comment: "CSV: Flesch reading ease"),
                    FleschKincaidGrade = table.Column<double>(type: "double precision", nullable: true, comment: "CSV: Flesch–Kincaid grade"),
                    GunningFog = table.Column<double>(type: "double precision", nullable: true, comment: "CSV: Gunning fog"),
                    ColemanLiau = table.Column<double>(type: "double precision", nullable: true, comment: "CSV: Coleman–Liau"),
                    ReadingTimeMinutes = table.Column<double>(type: "double precision", nullable: true, comment: "CSV: reading time (min, 238 wpm)"),
                    Flags = table.Column<List<string>>(type: "text[]", nullable: false, comment: "CSV: flags (empty = none)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BorcRankings",
                columns: table => new
                {
                    BorcCategory = table.Column<int>(type: "integer", nullable: false),
                    ArticleId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    RelevanceScore = table.Column<int>(type: "integer", nullable: false),
                    FindingCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorcRankings", x => new { x.ArticleId, x.BorcCategory });
                    table.ForeignKey(
                        name: "FK_BorcRankings_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ArticleId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ImpactFactor = table.Column<int>(type: "integer", nullable: false),
                    BorcCategory = table.Column<int>(type: "integer", nullable: false),
                    EvidenceExcerpt = table.Column<string>(type: "text", nullable: false),
                    RigorLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImpactFactorRankings",
                columns: table => new
                {
                    ImpactFactor = table.Column<int>(type: "integer", nullable: false),
                    ArticleId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    RelevanceScore = table.Column<int>(type: "integer", nullable: false),
                    FindingCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactFactorRankings", x => new { x.ArticleId, x.ImpactFactor });
                    table.ForeignKey(
                        name: "FK_ImpactFactorRankings_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetaSkillScores",
                columns: table => new
                {
                    ArticleId = table.Column<int>(type: "integer", nullable: false),
                    CriticalReasoningScore = table.Column<int>(type: "integer", nullable: false),
                    DecisionMakingScore = table.Column<int>(type: "integer", nullable: false),
                    ProblemSolvingScore = table.Column<int>(type: "integer", nullable: false),
                    CreativityScore = table.Column<int>(type: "integer", nullable: false),
                    CommunicationsScore = table.Column<int>(type: "integer", nullable: false),
                    CriticalReasoningJustification = table.Column<string>(type: "text", nullable: false),
                    DecisionMakingJustification = table.Column<string>(type: "text", nullable: false),
                    ProblemSolvingJustification = table.Column<string>(type: "text", nullable: false),
                    CreativityJustification = table.Column<string>(type: "text", nullable: false),
                    CommunicationsJustification = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaSkillScores", x => x.ArticleId);
                    table.ForeignKey(
                        name: "FK_MetaSkillScores_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMetrics_File_EngineVersion",
                table: "DocumentMetrics",
                columns: new[] { "File", "EngineVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMetrics_Status",
                table: "DocumentMetrics",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_ArticleId",
                table: "Findings",
                column: "ArticleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BorcRankings");

            migrationBuilder.DropTable(
                name: "DocumentMetrics");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "ImpactFactorRankings");

            migrationBuilder.DropTable(
                name: "MetaSkillScores");

            migrationBuilder.DropTable(
                name: "Articles");
        }
    }
}
