using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactsOfAI.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorsAndKeywordsArraysToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Articles");

            migrationBuilder.AddColumn<string[]>(
                name: "Authors",
                table: "Articles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "Keywords",
                table: "Articles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Authors",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "Articles");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PublishedAt",
                table: "Articles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
