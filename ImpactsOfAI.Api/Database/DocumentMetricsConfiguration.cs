using System.Linq.Expressions;
using ImpactsOfAI.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImpactsOfAI.Api.Database;

public sealed class DocumentMetricsConfiguration : IEntityTypeConfiguration<DocumentMetrics>
{
    public void Configure(EntityTypeBuilder<DocumentMetrics> b)
    {
        b.ToTable("DocumentMetrics");
        b.HasKey(x => x.Id);
 
        // identity
        b.Property(x => x.File).IsRequired().HasMaxLength(260).HasComment("CSV: file");
        b.Property(x => x.Status).IsRequired().HasMaxLength(32).HasComment("CSV: status");
        b.Property(x => x.EngineVersion).IsRequired().HasMaxLength(32).HasComment("CSV: engine version");
        b.Property(x => x.Pages).HasComment("CSV: pages");
        b.Property(x => x.Doi).HasMaxLength(255).HasComment("CSV: DOI (null = n/a)");
 
        // counts and scores: null means the engine reported "n/a (...)"
        void Count(Expression<Func<DocumentMetrics, int?>> p, string csv) =>
            b.Property(p).HasComment($"CSV: {csv}");
        void Score(Expression<Func<DocumentMetrics, double?>> p, string csv) =>
            b.Property(p).HasComment($"CSV: {csv}");
 
        Count(x => x.NumWords, "Num_Words");
        Count(x => x.NumCharWithSpaces, "Num_Char (with spaces)");
        Count(x => x.NumCharWithoutSpaces, "Num_Char (without spaces)");
        Count(x => x.NumStatements, "Num_Statements");
 
        Count(x => x.NumParagraphs, "Num_Paragraphs");
        Count(x => x.ParagraphsLeftOutNoTerminalPunctuation, "left out: no terminal punctuation");
        Count(x => x.ParagraphsLeftOutShortItemLists, "left out: lists of short items");
        Count(x => x.ParagraphsLeftOutRunInLabels, "left out: run-in labels standing alone");
        Count(x => x.ParagraphsLeftOutCreditLines, "left out: credit lines and sign-offs");
        Count(x => x.Headings, "headings");
 
        Count(x => x.NumParagraphs3Plus, "Num_Paragraphs_3plus");
        Count(x => x.Paragraphs3PlusLeftOutTwoOrFewerStatements, "Num_Paragraphs_3plus: left out, two or fewer statements");
 
        Count(x => x.NumObjects, "Num_Objects");
        Count(x => x.Logos, "logos");
        Count(x => x.Images, "images");
        Count(x => x.VectorFigures, "vector figures");
        Count(x => x.UncertainObjects, "uncertain objects");
 
        Count(x => x.NumCitations, "Num_Citations");
        Count(x => x.NumberedSeriesCitations, "numbered series");
        Count(x => x.AuthorYearCitations, "author–year");
        Count(x => x.BackReferences, "back-references");
 
        Count(x => x.FrontMatterWords, "front-matter words");
        Count(x => x.BackMatterWords, "back-matter words");
        Count(x => x.UncertainHyphens, "uncertain hyphens");
 
        Score(x => x.FleschReadingEase, "Flesch reading ease");
        Score(x => x.FleschKincaidGrade, "Flesch–Kincaid grade");
        Score(x => x.GunningFog, "Gunning fog");
        Score(x => x.ColemanLiau, "Coleman–Liau");
        Score(x => x.ReadingTimeMinutes, "reading time (min, 238 wpm)");
 
        // List<string> is a primitive collection: stored as JSON on SQL Server/SQLite, text[] on Npgsql.
        b.Property(x => x.Flags).IsRequired().HasComment("CSV: flags (empty = none)");
 
        // one row per file per engine version
        b.HasIndex(x => new { x.File, x.EngineVersion }).IsUnique();
        b.HasIndex(x => x.Status);
    }
}