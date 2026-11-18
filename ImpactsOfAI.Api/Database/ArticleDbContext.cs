using ImpactsOfAI.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ImpactsOfAI.Api.Database;

public class ArticleDbContext(DbContextOptions<ArticleDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles { get; set; }
    public DbSet<Finding> Findings { get; set; }
    public DbSet<ImpactFactorRanking> ImpactFactorRankings { get; set; }
    public DbSet<BorcRanking> BorcRankings { get; set; }
    public DbSet<MetaSkillScores> MetaSkillScores { get; set; }
    public DbSet<DocumentMetrics> DocumentMetrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DocumentMetricsConfiguration());

        modelBuilder.Entity<ImpactFactorRanking>()
            .HasKey(r => new { r.ArticleId, r.ImpactFactor });

        modelBuilder.Entity<BorcRanking>()
            .HasKey(r => new { r.ArticleId, r.BorcCategory });

        modelBuilder.Entity<MetaSkillScores>().HasKey(s => s.ArticleId);
        modelBuilder.Entity<MetaSkillScores>().HasOne(s => s.Article).WithOne().HasForeignKey<MetaSkillScores>(s => s.ArticleId);
    }
}