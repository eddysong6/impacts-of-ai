using System.Text.Json;
using System.Text.Json.Serialization;
using ImpactsOfAI.Api.Domain;
using ImpactsOfAI.Api.Engine.Verification;
using OpenAI.FineTuning;
using OpenAI.Responses;
#pragma warning disable OPENAI001

namespace ImpactsOfAI.Api.Engine;

public class AnalysisEngine(string articleContent, ResponsesClient responsesClient)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
        }
    };
    
    private static readonly ImpactFactor[] ImpactFactors =
    [
        ImpactFactor.NaturalEnvironment, ImpactFactor.Energy, ImpactFactor.Socioculture, ImpactFactor.Socioeconomics,
        ImpactFactor.HealthWellness, ImpactFactor.Education, ImpactFactor.GovernmentPolitics,
        ImpactFactor.Infrastructure
    ];

    private static readonly BorcCategory[] BorcCategories =
        [BorcCategory.Benefit, BorcCategory.Opportunity, BorcCategory.Challenge, BorcCategory.Risk];
    
    private ExcerptVerifier verifier = new(articleContent);

    public async Task<AnalyzedArticle> AnalyzeArticleAsync(CancellationToken cancellationToken)
    {
        FindingsResponse findingsResponse = null!;
        MetaSkillScoresResponse metaSkillScoresResponse = null!;

        async Task SetFindingsResponse() => findingsResponse = await ExtractFindingsAsync(cancellationToken);
        async Task SetMetaSkillScoresResponse() => metaSkillScoresResponse = await ScoreMetaSkillsAsync(cancellationToken);

        await Task.WhenAll(SetFindingsResponse(), SetMetaSkillScoresResponse()).WaitAsync(cancellationToken);

        FindingsResponse.Finding[] verifiedFindings = findingsResponse.Findings
            .Where(f => f.VerificationStatus != VerificationStatus.Dropped).ToArray();

        (CategoryAggregate<ImpactFactor>[] impactFactorRanking, CategoryAggregate<BorcCategory>[] borcCategoryRanking) = 
            RankCategories(verifiedFindings);

        (int compositeScore, RigorLevel overallRigor) = CalculateOverallRigor(verifiedFindings, metaSkillScoresResponse);

        return new AnalyzedArticle
        {
            ArticleContent = articleContent,
            FindingsResponse = findingsResponse,
            ImpactFactorRanking = impactFactorRanking,
            BorcCategoryRanking = borcCategoryRanking,
            MetaSkillScoresResponse = metaSkillScoresResponse,
            CompositeScore = compositeScore,
            OverallRigor = overallRigor
        };
    }

    private async Task<FindingsResponse> ExtractFindingsAsync(CancellationToken cancellationToken)
    {
        string userPrompt = $"""
                             Source text:
                             <<<
                             {articleContent}
                             >>>

                             Extract and tag every distinct finding per your instructions.
                             """;

        BinaryData schema = BinaryData.FromString(AnalysisEngineConstants.FindingsJsonSchema);

        CreateResponseOptions options = new()
        {
            Model = "gpt-5.6-terra",
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "extract_findings",
                    schema,
                    jsonSchemaIsStrict: true)
            }
        };

        options.InputItems.Add(ResponseItem.CreateSystemMessageItem(AnalysisEngineConstants.FindingsExtractionPrompt));
        options.InputItems.Add(ResponseItem.CreateUserMessageItem(userPrompt));

        ResponseResult response = await responsesClient.CreateResponseAsync(options, cancellationToken);

        FindingsResponse findingsResponse = JsonSerializer.Deserialize<FindingsResponse>(response.GetOutputText(), JsonSerializerOptions);
        
        foreach (FindingsResponse.Finding finding in findingsResponse.Findings)
        {
            ExcerptMatch match = verifier.Verify(finding.EvidenceExcerpt);

            finding.VerificationStatus = match.Kind switch
            {
                MatchKind.Exact => VerificationStatus.Verified,
                MatchKind.Fuzzy => VerificationStatus.Flagged,
                _ => VerificationStatus.Dropped
            };

            finding.EvidenceOffset = match.StartOffset;
        }

        return findingsResponse;
    }

    private async Task<MetaSkillScoresResponse> ScoreMetaSkillsAsync(CancellationToken cancellationToken)
    {
        string userPrompt = $"""
                      Source text:
                      <<<
                      {articleContent}
                      >>>

                      Score all five meta-skills per your instructions.
                      """;

        BinaryData schema = BinaryData.FromString(AnalysisEngineConstants.MetaSkillScoresJsonSchema);
		
        CreateResponseOptions options = new()
        {
            Model = "gpt-5.6-terra",
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "score_meta_skills",
                    schema,
                    jsonSchemaIsStrict: true)
            }
        };

        options.InputItems.Add(ResponseItem.CreateSystemMessageItem(AnalysisEngineConstants.MetaSkillScoringPrompt));
        options.InputItems.Add(ResponseItem.CreateUserMessageItem(userPrompt));

        ResponseResult response = await responsesClient.CreateResponseAsync(options, cancellationToken);

        MetaSkillScoresResponse metaSkillScoresResponse = JsonSerializer.Deserialize<MetaSkillScoresResponse>(response.GetOutputText(), JsonSerializerOptions);

        metaSkillScoresResponse.DecisionMaking = VerifyMetaSkillScore(metaSkillScoresResponse.DecisionMaking);
        metaSkillScoresResponse.Communications = VerifyMetaSkillScore(metaSkillScoresResponse.Communications);
        metaSkillScoresResponse.Creativity = VerifyMetaSkillScore(metaSkillScoresResponse.Creativity);
        metaSkillScoresResponse.CriticalReasoning = VerifyMetaSkillScore(metaSkillScoresResponse.CriticalReasoning);
        metaSkillScoresResponse.ProblemSolving = VerifyMetaSkillScore(metaSkillScoresResponse.ProblemSolving);
        
        return metaSkillScoresResponse;
    }
    
    private MetaSkillScoresResponse.MetaSkillScore VerifyMetaSkillScore(MetaSkillScoresResponse.MetaSkillScore metaSkillScore)
    {
        if (metaSkillScore.EvidenceExcerpts.Length == 0)
        {
            metaSkillScore.VerificationStatus = VerificationStatus.Dropped;
            return metaSkillScore;
        }
        
        foreach (string evidenceExcerpt in metaSkillScore.EvidenceExcerpts)
        {
            ExcerptMatch match = verifier.Verify(evidenceExcerpt);

            if (match.Kind == MatchKind.None)
            {
                metaSkillScore.VerificationStatus = VerificationStatus.Dropped;
                break;
            }

            if (match.Kind == MatchKind.Fuzzy)
            {
                metaSkillScore.VerificationStatus = VerificationStatus.Flagged;
            }
        }

        return metaSkillScore;
    }

    private static (CategoryAggregate<ImpactFactor>[] impactFactorRanking, CategoryAggregate<BorcCategory>[] borcCategoryRanking
        ) RankCategories(FindingsResponse.Finding[] findings)
    {
        CategoryAggregate<ImpactFactor>[] impactFactorRanking =
            AggregateCategory(findings, ImpactFactors, f => f.ImpactFactor);
		
        CategoryAggregate<BorcCategory>[] borcCategoryRanking =
            AggregateCategory(findings, BorcCategories, f => f.BorcCategory);

        return (impactFactorRanking, borcCategoryRanking);
    }
    
    private static CategoryAggregate<T>[] AggregateCategory<T>(FindingsResponse.Finding[] findings, T[] categories, Func<FindingsResponse.Finding, T> getCategoryField) where T : Enum
    {
        Dictionary<T, (int score, int count)> raw =
            new(categories.Select(c => new KeyValuePair<T, (int score, int count)>(c, (0, 0))));

        foreach (FindingsResponse.Finding finding in findings)
        {
            T category = getCategoryField(finding);
            raw[category] = (raw[category].score + (int)finding.Rigor + 1, raw[category].count + 1);
        }

        int maxScore = raw.Select(r => r.Value.score).Max();

        if (maxScore <= 0) maxScore = 1;

        return categories
            .Select(c => new CategoryAggregate<T>
            {
                Category = c,
                RelevanceScore = (int)Math.Round(((double)raw[c].score / maxScore) * 100),
                FindingCount = raw[c].count
            })
            .OrderByDescending(c => c.RelevanceScore)
            .ThenByDescending(c => c.FindingCount)
            .ToArray();
    }
    
    private static (int compositeScore, RigorLevel overallRigor) CalculateOverallRigor(FindingsResponse.Finding[] findings, MetaSkillScoresResponse metaSkillScores)
    {
        var skillParts = new (double weight, MetaSkillScoresResponse.MetaSkillScore score)[]
        {
            (0.25, metaSkillScores.CriticalReasoning),
            (0.15, metaSkillScores.DecisionMaking),
            (0.15, metaSkillScores.ProblemSolving),
            (0.025, metaSkillScores.Creativity),
            (0.025, metaSkillScores.Communications)
        };

        var verifiedSkillParts = skillParts.Where(p => p.score.VerificationStatus != VerificationStatus.Dropped).ToArray();

        double totalSkillPart = verifiedSkillParts.Length == 0
            ? 0
            : verifiedSkillParts.Sum(p => p.weight * p.score.Score) / verifiedSkillParts.Sum(p => p.weight) * 0.6;
        
        double avgRigorRaw = findings.Length > 0 ? findings.Average(f => (int)f.Rigor + 1) : 1;
        double scaledRigor = ((avgRigorRaw - 1) / 6) * 10;

        double composite = 0.4 * scaledRigor + totalSkillPart;

        RigorLevel overallRigor = (RigorLevel)Math.Min(6, (int)Math.Floor(composite / (10.0 / 7)));

        return ((int)Math.Round(composite * 100), overallRigor);
    }
}