using System.Collections.Concurrent;
using System.Text;
using ImpactsOfAI.Api.ApiKey;
using ImpactsOfAI.Api.Engine;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace ImpactsOfAI.Api.Endpoints.Article;

public static class ArticleEndpoints
{
    public static void MapArticlesEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/article").AddEndpointFilter<ApiKeyEndpointFilter>();

        group.MapPost("/{doiPrefix}/{doiSuffix}", CreateArticleAnalysis);
    }

    private static async Task<IResult> CreateArticleAnalysis(string doiPrefix, string doiSuffix, List<DocumentResult> descriptiveResults, IConfiguration configuration, HttpRequest request, HttpClient httpClient)
    {
		string doi = doiPrefix + "/" + doiSuffix;
		
		HttpResponseMessage sciteResponseMessage = await httpClient.GetAsync($"https://api.scite.ai/papers/{doi}");

		if (!sciteResponseMessage.IsSuccessStatusCode)
		{
			ProblemDetails problemDetails = new()
			{
				Status = (int)sciteResponseMessage.StatusCode,
				Title = "Citation retrieval failed",
				Detail = sciteResponseMessage.ReasonPhrase,
			};

			return Results.Problem(problemDetails);
		}

		SciteResponse sciteResponse = await sciteResponseMessage.Content.ReadFromJsonAsync<SciteResponse>() ?? throw new Exception();

		CitationDataResponse citationDataResponse = new()
		{
			Abstract = sciteResponse.Abstract,
			Doi = sciteResponse.Doi,
			Issue = int.Parse(sciteResponse.Issue),
			Journal = sciteResponse.Journal,
			PageMin = int.Parse(sciteResponse.Page.Split("-")[0]),
			PageMax = int.Parse(sciteResponse.Page.Split("-")[1]),
			Publisher = sciteResponse.Publisher,
			Title = sciteResponse.Title,
			Type = sciteResponse.Type,
			Volume = int.Parse(sciteResponse.Volume),
			Year = new DateOnly(sciteResponse.Year, 1, 1),
			Authors = sciteResponse.Authors.Select(a => $"{a.Family}; {a.Given}").ToArray()
		};
			
		// string content;
		//
		// using (StreamReader reader = new(request.Body, Encoding.UTF8))
		// {  
		// 	content = await reader.ReadToEndAsync();
		// }
		
		string[] contents = descriptiveResults.Select(r => r.Reconstruction.Blocks.Aggregate("", (current, block) => current + block.Text)).ToArray();
		ConcurrentBag<DataExtractionResponse> dataExtractionResponses = [];
		ResponsesClient responsesClient = new(configuration["OpenAIApiKey"]);

		await Task.WhenAll(contents.Select<string, Task>(async content =>
		{
			AnalysisEngine analysisEngine = new(content, responsesClient);
			AnalyzedArticle analyzedArticle = await analysisEngine.AnalyzeArticleAsync();

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
			
			dataExtractionResponses.Add(dataExtractionResponse);
		}));

		return Results.Ok(dataExtractionResponses);
    }
}