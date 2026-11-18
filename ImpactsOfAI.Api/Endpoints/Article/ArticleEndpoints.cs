using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using ImpactsOfAI.Api.ApiKey;
using ImpactsOfAI.Api.Database;
using ImpactsOfAI.Api.Domain;
using ImpactsOfAI.Api.Engine;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace ImpactsOfAI.Api.Endpoints.Article;

public static class ArticleEndpoints
{
    public static void MapArticlesEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/article").AddEndpointFilter<ApiKeyEndpointFilter>();

        group.MapPost("", CreateArticleAnalysis);
    }

    private static async Task<IResult> CreateArticleAnalysis(List<DocumentResult> descriptiveResults, IConfiguration configuration, HttpRequest request, HttpClient httpClient, ArticleDbContext dbContext)
    {
		// string doi = doiPrefix + "/" + doiSuffix;
		//
		// HttpResponseMessage sciteResponseMessage = await httpClient.GetAsync($"https://api.scite.ai/papers/{doi}");
		//
		// if (!sciteResponseMessage.IsSuccessStatusCode)
		// {
		// 	ProblemDetails problemDetails = new()
		// 	{
		// 		Status = (int)sciteResponseMessage.StatusCode,
		// 		Title = "Citation retrieval failed",
		// 		Detail = sciteResponseMessage.ReasonPhrase,
		// 	};
		//
		// 	return Results.Problem(problemDetails);
		// }
		//
		// SciteResponse sciteResponse = await sciteResponseMessage.Content.ReadFromJsonAsync<SciteResponse>() ?? throw new Exception();
		//
		// CitationDataResponse citationDataResponse = new()
		// {
		// 	Abstract = sciteResponse.Abstract,
		// 	Doi = sciteResponse.Doi,
		// 	Issue = sciteResponse.Issue,
		// 	Journal = sciteResponse.Journal,
		// 	Pages = sciteResponse.Page,
		// 	Publisher = sciteResponse.Publisher,
		// 	Title = sciteResponse.Title,
		// 	Type = sciteResponse.Type,
		// 	Volume = sciteResponse.Volume,
		// 	Year = sciteResponse.Year,
		// 	Authors = sciteResponse.Authors.Select(a => $"{a.Family}, {a.Given}").ToArray()
		// };
		
		// string content;
		//
		// using (StreamReader reader = new(request.Body, Encoding.UTF8))
		// {  
		// 	content = await reader.ReadToEndAsync();
		// }
		
		// string[] contents = descriptiveResults.Select(r => r.Reconstruction.Blocks.Aggregate("", (current, block) => current + block.Text)).ToArray();
		ConcurrentBag<(DocumentResult metrics, DataExtractionResponse data)> dataExtractionResponses = [];
		ResponsesClient responsesClient = new(configuration["OpenAIApiKey"]);

		using (CancellationTokenSource cancellationTokenSource = new())
		{
			CancellationToken cancellationToken = cancellationTokenSource.Token;
			ProblemDetails? problemDetails = null;

			try
			{
				await Task.WhenAll(descriptiveResults.Select(async Task (result) =>
				{
					CitationDataResponse? citationDataResponse = null;
					
					if (string.IsNullOrEmpty(result.Doi.Note))
					{
						HttpResponseMessage sciteResponseMessage = await httpClient.GetAsync($"https://api.scite.ai/papers/{result.Doi.Value}", cancellationToken);

						if (!sciteResponseMessage.IsSuccessStatusCode)
						{
							problemDetails = new ProblemDetails
							{
								Status = (int)sciteResponseMessage.StatusCode,
								Title = "Citation retrieval failed",
								Detail = sciteResponseMessage.ReasonPhrase,
							};
						
							await cancellationTokenSource.CancelAsync();
						}

						SciteResponse sciteResponse = await sciteResponseMessage.Content.ReadFromJsonAsync<SciteResponse>(cancellationToken) ?? throw new Exception();

						citationDataResponse = new CitationDataResponse
						{
							Abstract = sciteResponse.Abstract,
							Doi = sciteResponse.Doi,
							Issue = sciteResponse.Issue,
							Journal = sciteResponse.Journal,
							Pages = sciteResponse.Page,
							Publisher = sciteResponse.Publisher,
							Title = sciteResponse.Title,
							Type = sciteResponse.Type,
							Volume = sciteResponse.Volume,
							Year = sciteResponse.Year,
							Authors = sciteResponse.Authors.Select(a => $"{a.Family}, {a.Given}").ToArray()
						};
					}
				
					string content = result.Reconstruction.Blocks.Aggregate("", (current, block) => current + block.Text);
				
					AnalysisEngine analysisEngine = new(content, responsesClient);
					AnalyzedArticle analyzedArticle = await analysisEngine.AnalyzeArticleAsync(cancellationToken);

					DataExtractionResponse dataExtractionResponse = new()
					{
						CitationDataResponse = citationDataResponse,
						FindingsResponse = analyzedArticle.FindingsResponse,
						ImpactFactorRanking = analyzedArticle.ImpactFactorRanking,
						BorcCategoryRanking = analyzedArticle.BorcCategoryRanking,
						MetaSkillScoresResponse = analyzedArticle.MetaSkillScoresResponse,
						CompositeScore = analyzedArticle.CompositeScore,
						OverallRigor = analyzedArticle.OverallRigor
					};
				
					dataExtractionResponses.Add((result, dataExtractionResponse));
				})).WaitAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				return problemDetails != null ? Results.Problem(problemDetails) : Results.Ok();
			}
		}

		foreach ((DocumentResult metrics, DataExtractionResponse data) dataExtractionResponse in dataExtractionResponses)
		{
			Domain.Article article = new()
			{
				RigorLevel = dataExtractionResponse.data.OverallRigor,
				CompositeScore = dataExtractionResponse.data.CompositeScore,
				Keywords = ["TODO: add keywords"],
				Title = "test",
				Authors = ["test"],
				Publisher = "test",
				Summary = "test",
				SourceType = "test",
				Volume = "test",
				Issue = "test",
				Doi = "test",
				Journal = "test",
				Pages = "test",
				Year = 1234,
				URL = "test"
			};

			if (dataExtractionResponse.data.CitationDataResponse != null)
			{
				article.Title = dataExtractionResponse.data.CitationDataResponse.Title;
				article.Authors = dataExtractionResponse.data.CitationDataResponse.Authors;
				article.Publisher = dataExtractionResponse.data.CitationDataResponse.Publisher;
				article.Summary = dataExtractionResponse.data.CitationDataResponse.Abstract;
				article.SourceType = dataExtractionResponse.data.CitationDataResponse.Type;
				article.Volume = dataExtractionResponse.data.CitationDataResponse.Volume;
				article.Issue = dataExtractionResponse.data.CitationDataResponse.Issue;
				article.Doi = dataExtractionResponse.data.CitationDataResponse.Doi;
				article.Journal = dataExtractionResponse.data.CitationDataResponse.Journal;
				article.Pages = dataExtractionResponse.data.CitationDataResponse.Pages;
				article.Year = dataExtractionResponse.data.CitationDataResponse.Year;
			}

			foreach (FindingsResponse.Finding findingResponse in dataExtractionResponse.data.FindingsResponse.Findings)
			{
				Finding finding = new()
				{
					Article = article,
					Description = findingResponse.Summary,
					ImpactFactor = findingResponse.ImpactFactor,
					BorcCategory = findingResponse.BorcCategory,
					EvidenceExcerpt = findingResponse.EvidenceExcerpt,
					RigorLevel = findingResponse.Rigor
				};

				dbContext.Findings.Add(finding);
			}

			for (var i = 0; i < dataExtractionResponse.data.ImpactFactorRanking.Length; i++)
			{
				CategoryAggregate<ImpactFactor> aggregate = dataExtractionResponse.data.ImpactFactorRanking[i];

				ImpactFactorRanking impactFactorRanking = new()
				{
					ImpactFactor = aggregate.Category,
					Article = article,
					Rank = i,
					RelevanceScore = aggregate.RelevanceScore,
					FindingCount = aggregate.FindingCount
				};

				dbContext.ImpactFactorRankings.Add(impactFactorRanking);
			}

			for (var i = 0; i < dataExtractionResponse.data.BorcCategoryRanking.Length; i++)
			{
				CategoryAggregate<BorcCategory> aggregate = dataExtractionResponse.data.BorcCategoryRanking[i];

				BorcRanking borcRanking = new()
				{
					BorcCategory = aggregate.Category,
					Article = article,
					Rank = i,
					RelevanceScore = aggregate.RelevanceScore,
					FindingCount = aggregate.FindingCount
				};

				dbContext.BorcRankings.Add(borcRanking);
			}

			MetaSkillScores metaSkillScores = new()
			{
				Article = article,
				CriticalReasoningScore = dataExtractionResponse.data.MetaSkillScoresResponse.CriticalReasoning.Score,
				DecisionMakingScore = dataExtractionResponse.data.MetaSkillScoresResponse.DecisionMaking.Score,
				ProblemSolvingScore = dataExtractionResponse.data.MetaSkillScoresResponse.ProblemSolving.Score,
				CreativityScore = dataExtractionResponse.data.MetaSkillScoresResponse.Creativity.Score,
				CommunicationsScore = dataExtractionResponse.data.MetaSkillScoresResponse.Communications.Score,
				CriticalReasoningJustification =
					dataExtractionResponse.data.MetaSkillScoresResponse.CriticalReasoning.Justification,
				DecisionMakingJustification =
					dataExtractionResponse.data.MetaSkillScoresResponse.DecisionMaking.Justification,
				ProblemSolvingJustification =
					dataExtractionResponse.data.MetaSkillScoresResponse.ProblemSolving.Justification,
				CreativityJustification = dataExtractionResponse.data.MetaSkillScoresResponse.Creativity.Justification,
				CommunicationsJustification =
					dataExtractionResponse.data.MetaSkillScoresResponse.Communications.Justification
			};
			
			dbContext.MetaSkillScores.Add(metaSkillScores);
			
			// dbContext.DocumentMetrics.()
			
			DocumentMetrics documentMetrics = new()
			{
				Article = article,
				File = dataExtractionResponse.metrics.File.Name,
				Status = dataExtractionResponse.metrics.Status,
				EngineVersion = dataExtractionResponse.metrics.EngineVersion,
				Pages = dataExtractionResponse.metrics.Reconstruction.Pages,
				Doi = string.IsNullOrEmpty(dataExtractionResponse.metrics.Doi.Note) ? dataExtractionResponse.metrics.Doi.Value : null,
				NumWords = dataExtractionResponse.metrics.Core.NumWords.Value.Value,
				NumCharWithSpaces = dataExtractionResponse.metrics.Core.NumChar.Value.WithSpaces,
				NumCharWithoutSpaces = dataExtractionResponse.metrics.Core.NumChar.Value.WithoutSpaces,
				NumStatements = dataExtractionResponse.metrics.Core.NumStatements.Value.Value,
				NumParagraphs = dataExtractionResponse.metrics.Core.NumParagraphs.Value.Value,
				ParagraphsLeftOutNoTerminalPunctuation =
					dataExtractionResponse.metrics.Core.NumParagraphs.Excluded.NoTerminalPunctuation,
				ParagraphsLeftOutShortItemLists = dataExtractionResponse.metrics.Core.NumParagraphs.Excluded.ListsOfShortItems,
				ParagraphsLeftOutRunInLabels = dataExtractionResponse.metrics.Core.NumParagraphs.Excluded.RunInLabels,
				ParagraphsLeftOutCreditLines = dataExtractionResponse.metrics.Core.NumParagraphs.Excluded.CreditLinesAndSignOffs,
				Headings = dataExtractionResponse.metrics.Core.NumParagraphs.Excluded.Headings,
				NumParagraphs3Plus = dataExtractionResponse.metrics.Core.NumParagraphs3Plus.Value.Value,
				Paragraphs3PlusLeftOutTwoOrFewerStatements =
					dataExtractionResponse.metrics.Core.NumParagraphs3Plus.Excluded.TwoOrFewerStatements,
				NumObjects = dataExtractionResponse.metrics.Core.NumObjects.Value,
				Logos = dataExtractionResponse.metrics.Core.NumObjects.Logos,
				Images = dataExtractionResponse.metrics.Core.NumObjects.Images,
				VectorFigures = dataExtractionResponse.metrics.Core.NumObjects.VectorFigures,
				UncertainObjects = dataExtractionResponse.metrics.Core.NumObjects.Uncertain,
				NumCitations = dataExtractionResponse.metrics.Core.NumCitations.Value,
				NumberedSeriesCitations = dataExtractionResponse.metrics.Core.NumCitations.NumberedNotes,
				AuthorYearCitations = dataExtractionResponse.metrics.Core.NumCitations.AuthorYear,
				BackReferences = dataExtractionResponse.metrics.Core.NumCitations.BackReferences.Total,
				FrontMatterWords = dataExtractionResponse.metrics.Measures.Structure.Value.Value?.Exclusions.FrontMatterWords,
				BackMatterWords = dataExtractionResponse.metrics.Measures.Structure.Value.Value?.Exclusions.BackMatterWords,
				UncertainHyphens = dataExtractionResponse.metrics.Measures.Structure.Value.Value?.Exclusions.UncertainHyphens,
				FleschReadingEase = dataExtractionResponse.metrics.Measures.Readability.Value.Value?.FleschReadingEase.Value,
				FleschKincaidGrade = dataExtractionResponse.metrics.Measures.Readability.Value.Value?.FleschKincaidGrade.Value,
				GunningFog = dataExtractionResponse.metrics.Measures.Readability.Value.Value?.GunningFog.Value,
				ColemanLiau = dataExtractionResponse.metrics.Measures.Readability.Value.Value?.ColemanLiau.Value,
				ReadingTimeMinutes = dataExtractionResponse.metrics.Measures.Readability.Value.Value?.ReadingTimeMinutes,
				Flags = dataExtractionResponse.metrics.Flags
			};

			dbContext.DocumentMetrics.Add(documentMetrics);
			
			dbContext.Articles.Add(article);
		}
		
		await dbContext.SaveChangesAsync();

		return Results.Ok(dataExtractionResponses.Select(r => r.data));
    }
}