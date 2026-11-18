using ImpactsOfAI.Api.Domain;

namespace ImpactsOfAI.Api.Engine;

public class AnalyzedArticle
{
	public string ArticleContent { get; set; }
	public FindingsResponse FindingsResponse { get; set; }
	public CategoryAggregate<ImpactFactor>[] ImpactFactorRanking { get; set; }
	public CategoryAggregate<BorcCategory>[] BorcCategoryRanking { get; set; }
	public MetaSkillScoresResponse MetaSkillScoresResponse { get; set; }
	public int CompositeScore { get; set; }
	public RigorLevel OverallRigor { get; set; }
}

public record CategoryAggregate<T> where T : Enum
{
	public T Category { get; set; }
	public int RelevanceScore { get; set; }
	public int FindingCount { get; set; }
}

public record FindingsResponse
{
	public Finding[] Findings { get; set; }
	
	public record Finding
	{
		public string Summary { get; set; }
		public string EvidenceExcerpt { get; set; }
		public ImpactFactor ImpactFactor { get; set; }
		public BorcCategory BorcCategory { get; set; }
		public RigorLevel Rigor { get; set; }
		public VerificationStatus VerificationStatus { get; set; }
		public int EvidenceOffset { get; set; }
	}
}

public record MetaSkillScoresResponse
{
	public MetaSkillScore CriticalReasoning { get; set; }
	public MetaSkillScore DecisionMaking { get; set; }
	public MetaSkillScore ProblemSolving { get; set; }
	public MetaSkillScore Creativity { get; set; }
	public MetaSkillScore Communications { get; set; }

	public record MetaSkillScore
	{
		public int Score { get; set; }
		public string Justification { get; set; }
		public string[] EvidenceExcerpts { get; set; }
		public VerificationStatus VerificationStatus { get; set; }
	}
}

public enum VerificationStatus
{
	Verified,
	Flagged,
	Dropped
}

// public enum ImpactFactor
// {
// 	NaturalEnvironment,
// 	Energy,
// 	Socioculture,
// 	Socioeconomics,
// 	HealthWellness,
// 	Education,
// 	GovernmentPolitics,
// 	Infrastructure
// }
//
// public enum BorcCategory
// {
// 	Benefit,
// 	Opportunity,
// 	Challenge,
// 	Risk
// }
//
// public enum RigorLevel
// {
// 	ExtremelyLow,
// 	VeryLow,
// 	Low,
// 	Medium,
// 	High,
// 	VeryHigh,
// 	ExtremelyHigh
// }
