using ImpactsOfAI.Api.Domain;
using ImpactsOfAI.Api.Engine;

namespace ImpactsOfAI.Api.Endpoints.Article;

public record SciteResponse
{
	public List<Author> Authors { get; set; }
	public string Doi { get; set; }
	public List<object> EditorialNotices { get; set; }
	public int Id { get; set; }
	public List<string> Issns { get; set; }
	public string Issue { get; set; }
	public string Journal { get; set; }
	public string JournalSlug { get; set; }
	public int MemberId { get; set; }
	public List<string> NormalizedTypes { get; set; }
	public string Page { get; set; }
	public List<object> PreprintLinks { get; set; }
	public List<object> PublicationLinks { get; set; }
	public string Publisher { get; set; }
	public bool Retracted { get; set; }
	public string ShortJournal { get; set; }
	public string Slug { get; set; }
	public string Title { get; set; }
	public string Abstract { get; set; }
	public string Type { get; set; }
	public string Volume { get; set; }
	public int? Year { get; set; }
	
	public record Author
	{
		public string Affiliation { get; set; }
		public string AffiliationID { get; set; }
		public string AffiliationSlug { get; set; }
		public string AuthorID { get; set; }
		public string AuthorName { get; set; }
		public int AuthorSequenceNumber { get; set; }
		public string AuthorSlug { get; set; }
		public string Family { get; set; }
		public string Given { get; set; }
	}
}

public record CitationDataResponse
{
	public string Title { get; set; }
	public int? Year { get; set; }
	public string Publisher { get; set; }
	public string Abstract { get; set; }
	public string Type { get; set; }
	public string? Volume { get; set; }
	public string? Issue { get; set; }
	public string Doi { get; set; }
	public string Journal { get; set; }
	public string? Pages { get; set; }
	public string[] Authors { get; set; }
}

public record DataExtractionResponse
{
	public CitationDataResponse? CitationDataResponse { get; set; }
	public FindingsResponse FindingsResponse { get; set; }
	public CategoryAggregate<ImpactFactor>[] ImpactFactorRanking { get; set; }
	public CategoryAggregate<BorcCategory>[] BorcCategoryRanking { get; set; }
	public MetaSkillScoresResponse MetaSkillScoresResponse { get; set; }
	public int CompositeScore { get; set; }
	public RigorLevel OverallRigor { get; set; }
}