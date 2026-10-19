using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImpactsOfAI.Api.ApiKey;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Responses;

#pragma warning disable OPENAI001

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();

builder.Services.AddTransient<IApiKeyValidator, ApiKeyValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/article", async (IConfiguration configuration, HttpRequest request) =>
{
	string content;
	
	using (StreamReader reader = new(request.Body, Encoding.UTF8))
	{  
		content = await reader.ReadToEndAsync();
	}
	
	string systemPrompt = """
	                            You are extracting discrete arguments and findings from a research source about the impacts
	                            of AI, for a structured analysis pipeline. You are NOT summarizing the source as a whole —
	                            you are pulling out each individually stated claim, argument, or finding, one at a time.
	                            Each finding MUST be self-contained and broadly applicable — if a finding is not understandable without having read the source,
	                            cannot be generalized beyond the specific source, or refers to something from another finding, it is evidence
	                            to support a finding, not the finding itself.

	                            For each finding you extract:
	                               
	                            1. Write a summary that describes ONLY that finding, as a self-contained fact-claim. Do not
	                               write "the article argues" or "the source states" — state the finding directly, as if it
	                               stood alone. Do not let your understanding of the source's other findings, or your sense of
	                               the source's overall thesis, shape how you characterize this individual finding. The tense and qualification
	                               of uncertainty in the summary should reflect the finding's BORC category:
	                               
	                               Benefit/Challenge: present (example: contributes, is associated with, etc.)
	                               Opportunity/Risk: uncertain future (example: may contribute to, may be associated with, etc.)
	                               
	                               (what the BORC categories mean will be explained further below)

	                            2. Quote or closely paraphrase (<=40 words) the specific text this finding is grounded in.

	                            3. Classify the finding's impact factor using ONLY these eight categories: natural
	                               environment, energy, socioculture, socioeconomics, health/wellness, education,
	                               government/politics, infrastructure, using the following category definitions:
	                               
	                               Natural environment:
	                            	   Definition: Ecological/biophysical effects: emissions, land/water use, biodiversity, pollution, waste	
	                            	   Clarification of common confusion vs. Energy: environment covers downstream ecological consequences, not power generation/consumption itself
	                               Energy:
	                            	   Definition: Power generation, consumption, grid capacity, energy sourcing/efficiency
	                            	   Clarification of common confusion vs. Infrastructure: energy is about the power itself, not the hardware/systems that use it
	                               Socioculture:
	                            	   Definition: Shared beliefs, norms, identity, relationships, community, information ecosystem/media
	                            	   Clarification of common confusion vs. Socioeconomics: socioculture is how people relate and believe, not who has money or work
	                               Socioeconomics:
	                            	   Definition: Distribution of economic resources, labor, jobs, wealth, inequality, market structure	
	                            	   Clarification of common confusion vs. Health: cost/access to healthcare is socioeconomic; health outcomes are Health/Wellness
	                               Health/wellness:
	                            	   Definition: Physical or mental health outcomes, wellbeing, medical/healthcare delivery	
	                            	   Clarification of common confusion: An environmental exposure that causes a health outcome → tag the human-outcome finding Health/Wellness, tag the ecosystem-level finding Natural Environment
	                               Education:
	                            	   Definition: Learning, teaching, schools, curricula, skill-building, academic institutions	
	                            	   Clarification of common confusion vs. Socioeconomics: labor retraining framed as skills-in-the-abstract → Education; framed as wages/employment → Socioeconomics
	                               Government/politics:
	                            	   Definition: Policy, regulation, law, elections, governance, political discourse	
	                            	   Clarification of common confusion: Education policy is Government/Politics; classroom-level teaching practice is Education
	                               Infrastructure:
	                            	   Definition: Physical/digital built systems: data centers, compute, transportation, utilities, connectivity	
	                            	   Clarification of common confusion: The hardware/systems, not the power flowing through them (that's Energy)

	                            4. Classify the finding's BORC category using this 2x2 logic ({positive, negative} x {actual, potential}):

	                               Positive-Actual: Benefit
	                               Positive-Potential: Opportunity
	                               Negative-Actual: Challenge
	                               Negative-Potential: Risk

	                               Base "actual vs. potential" on how the SOURCE frames it (tense, hedging
	                               language like "could" vs. "has"), not on your own judgment of plausibility.

	                            5. Rate the finding's rigor using this 7-point scale: 

	                               Extremely low: Bare assertion; no evidence or citation; may be internally incoherent
	                               Very low: Vague/unsupported gesture at evidence ("studies show..." with nothing specific); anecdotal only
	                               Low: Some evidence, but thin — a single example, missing method/sample details
	                               Medium: Reasonable evidence and method, but notable gaps or unaddressed limitations
	                               High: Solid evidence, clear method, engages at least one counterargument or limitation
	                               Very high: Strong converging evidence, rigorous method, explicit engagement with limitations
	                               Extremely high: Multiple independent lines of evidence, transparent/replicable method, quantified uncertainty, thorough counterargument engagement

	                               Base this only on the evidence given for THIS finding, not on the source's overall credibility.

	                            Extract every distinct finding you can identify, including minor ones. Do not merge multiple
	                            distinct claims into one finding, and do not split a single claim into multiple findings.
	                            Ignore formatting artifacts from document conversion (stray line breaks, header/footer text,
	                            page numbers, citation markers) — these are not findings.
	                            """;

	string userPrompt = $"""
	                     Source text:
	                     <<<
	                     {content}
	                     >>>

	                     Extract and tag every distinct finding per your instructions.
	                     """;

	ResponsesClient responsesClient = new(configuration["OpenAIApiKey"]);

	BinaryData schema = BinaryData.FromString(
		"""
		{
		  "type": "object",
		  "properties": {
		    "findings": {
		      "type": "array",
		      "items": {
		        "type": "object",
		        "properties": {
		          "summary": {
		            "type": "string",
		            "description": "Self-contained description of the specific argument or finding. No meta-references to 'the article' or 'the source' — state the finding as a fact-claim on its own."
		          },
		          "evidence_excerpt": {
		            "type": "string",
		            "description": "A short verbatim quote or close paraphrase (<= 40 words) from the source that this finding is grounded in. Used for auditability, not for display."
		          },
		          "impact_factor": {
		            "type": "string",
		            "enum": [
		              "natural_environment",
		              "energy",
		              "socioculture",
		              "socioeconomics",
		              "health_wellness",
		              "education",
		              "government_politics",
		              "infrastructure"
		            ]
		          },
		          "borc_category": {
		            "type": "string",
		            "enum": ["benefit", "opportunity", "risk", "challenge"]
		          },
		          "rigor": {
		            "type": "string",
		            "enum": [
		              "extremely_low",
		              "very_low",
		              "low",
		              "medium",
		              "high",
		              "very_high",
		              "extremely_high"
		            ]
		          }
		        },
		        "required": ["summary", "evidence_excerpt", "impact_factor", "borc_category", "rigor"],
		        "additionalProperties": false
		      }
		    }
		  },
		  "required": ["findings"],
		  "additionalProperties": false
		}
		""");

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

	options.InputItems.Add(ResponseItem.CreateSystemMessageItem(systemPrompt));
	options.InputItems.Add(ResponseItem.CreateUserMessageItem(userPrompt));

	ResponseResult response = await responsesClient.CreateResponseAsync(options);

	JsonSerializerOptions jsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		Converters =
		{
			new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
		}
	};

	FindingsResponse findingsResponse = JsonSerializer.Deserialize<FindingsResponse>(response.GetOutputText(), jsonSerializerOptions);

	ImpactFactor[] impactFactors =
	[
		ImpactFactor.NaturalEnvironment, ImpactFactor.Energy, ImpactFactor.Socioculture, ImpactFactor.Socioeconomics,
		ImpactFactor.HealthWellness, ImpactFactor.Education, ImpactFactor.GovernmentPolitics,
		ImpactFactor.Infrastructure
	];

	BorcCategory[] borcCategories =
		[BorcCategory.Benefit, BorcCategory.Opportunity, BorcCategory.Challenge, BorcCategory.Risk];

	CategoryAggregate<ImpactFactor>[] impactFactorRanking =
		AggregateCategory(findingsResponse.Findings, impactFactors, f => f.ImpactFactor);
	
	CategoryAggregate<BorcCategory>[] borcCategoryRanking =
		AggregateCategory(findingsResponse.Findings, borcCategories, f => f.BorcCategory);

	systemPrompt = """
	               You are scoring a research source about the impacts of AI on five meta-skills it demonstrates
	               in its own argumentation. Score the SOURCE AS A WHOLE — this is a holistic judgment, not a
	               per-finding one. For each skill, give an integer 0-10 and a justification (<=40 words) that
	               cites specific textual evidence for the score you gave.
	               
	               Critical reasoning: does the source support its arguments with sufficient evidence and
	               elaboration for the conclusions to logically follow? Consider the following rubric anchors:
	               
	                  Critical reasoning score rubric anchors:
	                     Score of 2 (low): Claims made with little to no supporting evidence; logical gaps throughout
	                     Score of 5 (mid): Some claims supported, some not; reasoning mostly follows but with unaddressed leaps
	                     Score of 8 (high): Most claims tied to evidence; the chain from evidence to conclusion is clear and consistent
	               
	               Decision making: does the source make clear claims and clearly interpret what its evidence
	               means? Consider the following rubric anchors:
	               
	                  Decision making score rubric anchors:
	                     Score of 2 (low): Claims are vague or hedged to the point of meaninglessness; evidence isn't interpreted
	                     Score of 5 (mid): Claims are stated, but interpretation of what the evidence means is inconsistent
	                     Score of 8 (high): Claims are clearly stated and the source explicitly interprets what its evidence implies
	                  
	               Problem solving: does the source address real-world issues and engage with counterarguments
	               or limitations? Consider the following rubric anchors:
	               
	                  Problem solving score rubric anchors:
	                     Score of 2 (low): Stays abstract/theoretical; no counterarguments or practical implications addressed
	                     Score of 5 (mid): Addresses real-world implications or counterarguments, not both, and only superficially
	                     Score of 8 (high): Explicitly engages real-world application and meaningfully addresses counterarguments/limitations
	               
	               Creativity: does the source show ingenuity in its arguments, solutions, or methods, versus
	               restating conventional framing?
	               
	                  Creativity score rubric anchors:
	                     Score of 2 (low): Purely conventional; restates common framing or knowledge
	                     Score of 5 (mid): Some novel angle, example, or method, but largely conventional overall
	                     Score of 8 (high): Notably original framing, novel methodology, or an inventive argument/solution
	                  
	               Communications: is the source clearly written and readable? Ignore formatting artifacts from
	               document conversion (stray line breaks, running headers/footers, page numbers, broken citation
	               markers) — judge the underlying prose, not conversion noise.
	               
	                  Communications score rubric anchors:
	                     Score of 2 (low): Structure is confusing, jargon unexplained, hard to follow independent of formatting noise
	                     Score of 5 (mid): Generally followable but uneven — some sections clear, others dense or disorganized
	                     Score of 8 (high): Clear structure, accessible language for the audience, ideas flow logically
	               """;
	
	userPrompt = $"""
	             Source text:
	             <<<
	             {content}
	             >>>
	             
	             Score all five meta-skills per your instructions.
	             """;

	schema = BinaryData.FromString(
		"""
		{
		  "type": "object",
		  "properties": {
		    "critical_reasoning": { "type": "object", "properties": {
		      "score": { "type": "integer", "minimum": 0, "maximum": 10 },
		      "justification": { "type": "string" }
		    }, "required": ["score", "justification"], "additionalProperties": false },
		    "decision_making": { "type": "object", "properties": {
		      "score": { "type": "integer", "minimum": 0, "maximum": 10 },
		      "justification": { "type": "string" }
		    }, "required": ["score", "justification"], "additionalProperties": false },
		    "problem_solving": { "type": "object", "properties": {
		      "score": { "type": "integer", "minimum": 0, "maximum": 10 },
		      "justification": { "type": "string" }
		    }, "required": ["score", "justification"], "additionalProperties": false },
		    "creativity": { "type": "object", "properties": {
		      "score": { "type": "integer", "minimum": 0, "maximum": 10 },
		      "justification": { "type": "string" }
		    }, "required": ["score", "justification"], "additionalProperties": false },
		    "communications": { "type": "object", "properties": {
		      "score": { "type": "integer", "minimum": 0, "maximum": 10 },
		      "justification": { "type": "string" }
		    }, "required": ["score", "justification"], "additionalProperties": false }
		  },
		  "required": ["critical_reasoning", "decision_making", "problem_solving", "creativity", "communications"],
		  "additionalProperties": false
		}
		""");
	
	options = new CreateResponseOptions
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

	options.InputItems.Add(ResponseItem.CreateSystemMessageItem(systemPrompt));
	options.InputItems.Add(ResponseItem.CreateUserMessageItem(userPrompt));

	response = await responsesClient.CreateResponseAsync(options);

	MetaSkillScoresResponse metaSkillScoresResponse = JsonSerializer.Deserialize<MetaSkillScoresResponse>(response.GetOutputText(), jsonSerializerOptions);

	(int, RigorLevel) overallRigor = CalculateOverallRigor(findingsResponse.Findings, metaSkillScoresResponse);

	DataExtractionResponse dataExtractionResponse = new()
	{
		FindingsResponse = findingsResponse,
		ImpactFactorRanking = impactFactorRanking,
		BorcCategoryRanking = borcCategoryRanking,
		MetaSkillScoresResponse = metaSkillScoresResponse,
		CompositeScore = overallRigor.Item1,
		OverallRigor = overallRigor.Item2
	};

	return dataExtractionResponse;

	CategoryAggregate<T>[] AggregateCategory<T>(FindingsResponse.Finding[] findings, T[] categories, Func<FindingsResponse.Finding, T> getCategoryField) where T : Enum
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

	(int, RigorLevel) CalculateOverallRigor(FindingsResponse.Finding[] findings, MetaSkillScoresResponse metaSkillScores)
	{
		double avgRigorRaw = findings.Length > 0 ? findings.Average(f => (int)f.Rigor + 1) : 1;
		double scaledRigor = ((avgRigorRaw - 1) / 6) * 10;

		double composite =
			0.4 * scaledRigor +
			0.25 * metaSkillScores.CriticalReasoning.Score +
			0.15 * metaSkillScores.DecisionMaking.Score +
			0.15 * metaSkillScores.ProblemSolving.Score +
			0.025 * metaSkillScores.Creativity.Score +
			0.025 * metaSkillScores.Communications.Score;

		RigorLevel overallRigor = (RigorLevel)Math.Min(6, (int)Math.Floor(composite / (10.0 / 7)));

		return ((int)Math.Round(composite * 100), overallRigor);
	}
}).AddEndpointFilter<ApiKeyEndpointFilter>();

app.Run();

record DataExtractionResponse
{
	public FindingsResponse FindingsResponse { get; set; }
	public CategoryAggregate<ImpactFactor>[] ImpactFactorRanking { get; set; }
	public CategoryAggregate<BorcCategory>[] BorcCategoryRanking { get; set; }
	public MetaSkillScoresResponse MetaSkillScoresResponse { get; set; }
	public int CompositeScore { get; set; }
	public RigorLevel OverallRigor { get; set; }
}

record CategoryAggregate<T> where T : Enum
{
	public T Category { get; set; }
	public int RelevanceScore { get; set; }
	public int FindingCount { get; set; }
}

record FindingsResponse
{
	public Finding[] Findings { get; set; }
	
	public record Finding
	{
		public string Summary { get; set; }
		public string EvidenceExcerpt { get; set; }
		public ImpactFactor ImpactFactor { get; set; }
		public BorcCategory BorcCategory { get; set; }
		public RigorLevel Rigor { get; set; }
	}
}

record MetaSkillScoresResponse
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
	}
}

enum ImpactFactor
{
	NaturalEnvironment,
	Energy,
	Socioculture,
	Socioeconomics,
	HealthWellness,
	Education,
	GovernmentPolitics,
	Infrastructure
}

enum BorcCategory
{
	Benefit,
	Opportunity,
	Challenge,
	Risk
}

enum RigorLevel
{
	ExtremelyLow,
	VeryLow,
	Low,
	Medium,
	High,
	VeryHigh,
	ExtremelyHigh
}