namespace ImpactsOfAI.Api.Domain;

public class Article
{
    public int Id { get; init; }
    public string Title { get; set; }
    public string[] Authors { get; set; }
    public string Publisher { get; set; }
    public string URL { get; set; }
    public RigorLevel RigorLevel { get; set; }
    public string Summary { get; set; }
    public string[] Keywords { get; set; }
    public string SourceType { get; set; }
    public int CompositeScore { get; set; }
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Doi { get; set; }
    public string Journal { get; set; }
    public string? Pages { get; set; }
    public int? Year { get; set; }

    public List<Finding> Findings { get; } = [];
    public List<ImpactFactorRanking> ImpactFactorRankings { get; } = [];
    public List<BorcRanking> BorcRankings { get; } = [];
}

public class Finding
{
    public int Id { get; init; }
    public int ArticleId { get; set; }
    public Article Article { get; set; }
    public string Description { get; set; }
    public ImpactFactor ImpactFactor { get; set; }
    public BorcCategory BorcCategory { get; set; }
    public string EvidenceExcerpt { get; set; }
    public RigorLevel RigorLevel { get; set; }
}

public class ImpactFactorRanking
{
    public ImpactFactor ImpactFactor { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; }
    public int Rank { get; set; }
    public int RelevanceScore { get; set; }
    public int FindingCount { get; set; }
}

public class BorcRanking
{
    public BorcCategory BorcCategory { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; }
    public int Rank { get; set; }
    public int RelevanceScore { get; set; }
    public int FindingCount { get; set; }
}

public class MetaSkillScores
{
    public int ArticleId { get; set; }
    public Article Article { get; set; }
    public int CriticalReasoningScore { get; set; }
    public int DecisionMakingScore { get; set; }
    public int ProblemSolvingScore { get; set; }
    public int CreativityScore { get; set; }
    public int CommunicationsScore { get; set; }
    public string CriticalReasoningJustification { get; set; }
    public string DecisionMakingJustification { get; set; }
    public string ProblemSolvingJustification { get; set; }
    public string CreativityJustification { get; set; }
    public string CommunicationsJustification { get; set; }
}

public class DocumentMetrics
{
    public int Id { get; set; }
    
    public int ArticleId { get; set; }
    public Article Article { get; set; }
 
    // --- identity (always present) ---
    /// <summary>CSV "file"</summary>
    public string File { get; set; }
    /// <summary>CSV "status" (e.g. "ok")</summary>
    public string Status { get; set; }
    /// <summary>CSV "engine version" (e.g. "0.7.6")</summary>
    public string EngineVersion { get; set; }
    /// <summary>CSV "pages"</summary>
    public int Pages { get; set; }
    /// <summary>CSV "DOI". Null when the CSV says "n/a (no DOI found)".</summary>
    public string? Doi { get; set; }
 
    // --- Num_Words / Num_Char / Num_Statements ---
    public int? NumWords { get; set; }                 // Num_Words
    public int? NumCharWithSpaces { get; set; }        // Num_Char (with spaces)
    public int? NumCharWithoutSpaces { get; set; }     // Num_Char (without spaces)
    public int? NumStatements { get; set; }            // Num_Statements
 
    // --- Num_Paragraphs and what was left out of it ---
    public int? NumParagraphs { get; set; }                          // Num_Paragraphs
    public int? ParagraphsLeftOutNoTerminalPunctuation { get; set; } // left out: no terminal punctuation
    public int? ParagraphsLeftOutShortItemLists { get; set; }        // left out: lists of short items
    public int? ParagraphsLeftOutRunInLabels { get; set; }           // left out: run-in labels standing alone
    public int? ParagraphsLeftOutCreditLines { get; set; }           // left out: credit lines and sign-offs
    public int? Headings { get; set; }                               // headings
 
    // --- Num_Paragraphs_3plus ---
    public int? NumParagraphs3Plus { get; set; }                     // Num_Paragraphs_3plus
    public int? Paragraphs3PlusLeftOutTwoOrFewerStatements { get; set; } // Num_Paragraphs_3plus: left out, two or fewer statements
 
    // --- Num_Objects ---
    public int? NumObjects { get; set; }               // Num_Objects
    public int? Logos { get; set; }                    // logos
    public int? Images { get; set; }                   // images
    public int? VectorFigures { get; set; }            // vector figures
    public int? UncertainObjects { get; set; }         // uncertain objects
 
    // --- Num_Citations ---
    public int? NumCitations { get; set; }             // Num_Citations
    public int? NumberedSeriesCitations { get; set; }  // numbered series
    public int? AuthorYearCitations { get; set; }      // author–year
    public int? BackReferences { get; set; }           // back-references
 
    // --- document structure ---
    public int? FrontMatterWords { get; set; }         // front-matter words
    public int? BackMatterWords { get; set; }          // back-matter words
    public int? UncertainHyphens { get; set; }         // uncertain hyphens
 
    // --- readability (n/a, i.e. null, when the document has no statements) ---
    public double? FleschReadingEase { get; set; }     // Flesch reading ease
    public double? FleschKincaidGrade { get; set; }    // Flesch–Kincaid grade
    public double? GunningFog { get; set; }            // Gunning fog
    public double? ColemanLiau { get; set; }           // Coleman–Liau
    public double? ReadingTimeMinutes { get; set; }    // reading time (min, 238 wpm)
 
    /// <summary>CSV "flags". Empty when the CSV says "none".</summary>
    public List<string> Flags { get; set; } = [];
}

public enum BorcCategory
{
    Benefit,
    Opportunity,
    Challenge,
    Risk
}

public enum ImpactFactor
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

public enum RigorLevel
{
    ExtremelyLow,
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh,
    ExtremelyHigh
}
