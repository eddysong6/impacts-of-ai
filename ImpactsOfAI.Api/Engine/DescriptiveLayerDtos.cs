// DTOs for the text-metrics engine batch output (engineVersion 0.7.6).
// Target: .NET 8+ / System.Text.Json. Every property carries an explicit
// [JsonPropertyName], so no naming policy is needed in JsonSerializerOptions.
//
// Usage:
//   var docs = JsonSerializer.Deserialize<List<DocumentResult>>(json)!;
//   int words = docs[0].Core.NumWords.Value.Value;          // NaOr<int>
//   if (!docs[0].Core.NumWords.Value.HasValue) { /* "n/a (unsupported script)" */ }

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImpactsOfAI.Api.Engine;

#region "n/a (reason)" wrapper

/// <summary>
/// A value the engine reports either as a JSON value of type T or as a string
/// "n/a (reason)" when it does not apply (no statements, unsupported script, …).
/// </summary>
[JsonConverter(typeof(NaOrConverterFactory))]
public readonly struct NaOr<T>
{
    private NaOr(T? value, string? notApplicableReason)
    {
        Value = value;
        NotApplicableReason = notApplicableReason;
    }

    /// <summary>The value; default(T) when <see cref="HasValue"/> is false.</summary>
    public T? Value { get; }

    /// <summary>The engine's "n/a (…)" text, or null when a value is present.</summary>
    public string? NotApplicableReason { get; }

    public bool HasValue => NotApplicableReason is null;

    public static NaOr<T> Of(T value) => new(value, null);
    public static NaOr<T> NotApplicable(string reason) => new(default, reason);

    public static implicit operator NaOr<T>(T value) => Of(value);

    public override string ToString() => HasValue ? Value?.ToString() ?? string.Empty : NotApplicableReason!;
}

public sealed class NaOrConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(NaOr<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(NaOrConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}

public sealed class NaOrConverter<T> : JsonConverter<NaOr<T>>
{
    public override NaOr<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? NaOr<T>.NotApplicable(reader.GetString()!)
            : NaOr<T>.Of(JsonSerializer.Deserialize<T>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, NaOr<T> value, JsonSerializerOptions options)
    {
        if (value.HasValue) JsonSerializer.Serialize(writer, value.Value, options);
        else writer.WriteStringValue(value.NotApplicableReason);
    }
}

#endregion

#region Root

/// <summary>One element of the top-level array: the result for one PDF.</summary>
public sealed class DocumentResult
{
    [JsonPropertyName("engineVersion")] public string EngineVersion { get; set; } = "";
    [JsonPropertyName("file")] public SourceFile File { get; set; } = new();
    [JsonPropertyName("doi")] public DoiInfo Doi { get; set; } = new();
    [JsonPropertyName("flags")] public List<string> Flags { get; set; } = [];

    /// <summary>Rule descriptions keyed by topic ("reconstruction", "hyphens", "pdfjs", …).</summary>
    [JsonPropertyName("decisions")] public Dictionary<string, string> Decisions { get; set; } = [];

    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("reconstruction")] public Reconstruction Reconstruction { get; set; } = new();
    [JsonPropertyName("mainBody")] public MainBody MainBody { get; set; } = new();
    [JsonPropertyName("core")] public CoreMetrics Core { get; set; } = new();
    [JsonPropertyName("measures")] public Measures Measures { get; set; } = new();
}

public sealed class SourceFile
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("bytes")] public long Bytes { get; set; }
    [JsonPropertyName("pages")] public int Pages { get; set; }
}

public sealed class DoiInfo
{
    /// <summary>The DOI, or "n/a (no DOI found)".</summary>
    [JsonPropertyName("value")] public string Value { get; set; } = "";
    [JsonPropertyName("note")] public string Note { get; set; } = "";
}

#endregion

#region Reconstruction

public sealed class Reconstruction
{
    [JsonPropertyName("pages")] public int Pages { get; set; }
    [JsonPropertyName("stats")] public ReconstructionStats Stats { get; set; } = new();
    [JsonPropertyName("blocks")] public List<Block> Blocks { get; set; } = [];
    [JsonPropertyName("removedFurniture")] public RemovedFurniture RemovedFurniture { get; set; } = new();
    [JsonPropertyName("knownHyphenated")] public List<string> KnownHyphenated { get; set; } = [];
    [JsonPropertyName("hyphenation")] public HyphenationSummary Hyphenation { get; set; } = new();

    /// <summary>Printed page number per PDF page, or "n/a (no printed page number found)".</summary>
    [JsonPropertyName("pageLabels")] public List<string> PageLabels { get; set; } = [];

    [JsonPropertyName("footnoteBlocks")] public int FootnoteBlocks { get; set; }
    [JsonPropertyName("firstLineIndentShare")] public double FirstLineIndentShare { get; set; }
    [JsonPropertyName("ambiguousPageJoins")] public List<string> AmbiguousPageJoins { get; set; } = [];

    /// <summary>Always empty in the sample; element shape unknown.</summary>
    [JsonPropertyName("flushContinuations")] public List<JsonElement> FlushContinuations { get; set; } = [];
}

public sealed class ReconstructionStats
{
    [JsonPropertyName("bodyFontSize")] public double BodyFontSize { get; set; }
    [JsonPropertyName("linePitch")] public double LinePitch { get; set; }
    [JsonPropertyName("bodyLeft")] public double BodyLeft { get; set; }
    [JsonPropertyName("bodyRight")] public double BodyRight { get; set; }
    [JsonPropertyName("thresholds")] public Thresholds Thresholds { get; set; } = new();
}

public sealed class Thresholds
{
    [JsonPropertyName("LINE_Y_TOL")] public double LineYTol { get; set; }
    [JsonPropertyName("SEGMENT_GAP_EM")] public double SegmentGapEm { get; set; }
    [JsonPropertyName("WORD_GAP_EM")] public double WordGapEm { get; set; }
    [JsonPropertyName("SUP_RATIO")] public double SupRatio { get; set; }
    [JsonPropertyName("GUTTER_EM")] public double GutterEm { get; set; }
    [JsonPropertyName("STACK_GAP_PITCH")] public double StackGapPitch { get; set; }
    [JsonPropertyName("PARA_GAP_PITCH")] public double ParaGapPitch { get; set; }
    [JsonPropertyName("INDENT_EM")] public double IndentEm { get; set; }
    [JsonPropertyName("FULL_WIDTH_FRAC")] public double FullWidthFrac { get; set; }
    [JsonPropertyName("SHORT_EM")] public double ShortEm { get; set; }
    [JsonPropertyName("SHORT_FRAC")] public double ShortFrac { get; set; }
    [JsonPropertyName("FLUSH_LEFT_FRAC")] public double FlushLeftFrac { get; set; }
    [JsonPropertyName("FONT_DIFF_PT")] public double FontDiffPt { get; set; }
    [JsonPropertyName("FURNITURE_ZONE")] public double FurnitureZone { get; set; }
    [JsonPropertyName("RUNNING_SHARE")] public double RunningShare { get; set; }
    [JsonPropertyName("FOOTNOTE_RATIO")] public double FootnoteRatio { get; set; }
    [JsonPropertyName("MIN_LETTERS")] public int MinLetters { get; set; }
    [JsonPropertyName("fullTolPt")] public double FullTolPt { get; set; }
    [JsonPropertyName("shortTolPt")] public double ShortTolPt { get; set; }
    [JsonPropertyName("indentTolPt")] public double IndentTolPt { get; set; }
}

public sealed class Block
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("endPage")] public int EndPage { get; set; }

    /// <summary>See <see cref="BlockCategories"/>.</summary>
    [JsonPropertyName("category")] public string Category { get; set; } = "";

    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("lines")] public int Lines { get; set; }
    [JsonPropertyName("fontSize")] public double FontSize { get; set; }
    [JsonPropertyName("bold")] public bool Bold { get; set; }
    [JsonPropertyName("list")] public bool List { get; set; }
    [JsonPropertyName("firstLineIndent")] public bool FirstLineIndent { get; set; }
    [JsonPropertyName("crossesPageBreak")] public bool CrossesPageBreak { get; set; }
    [JsonPropertyName("crossesColumnBreak")] public bool CrossesColumnBreak { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = "";
}

/// <summary>Block category values seen in the output.</summary>
public static class BlockCategories
{
    public const string FrontMatter = "frontMatter";
    public const string Body = "body";
    public const string Heading = "heading";
    public const string BackMatter = "backMatter";
    public const string NoteEntry = "noteEntry";
    public const string References = "references";
    public const string Empty = "empty";
}

public sealed class RemovedFurniture
{
    [JsonPropertyName("headers")] public int Headers { get; set; }
    [JsonPropertyName("footers")] public int Footers { get; set; }
    [JsonPropertyName("pageNumbers")] public int PageNumbers { get; set; }

    /// <summary>Always empty in the sample; element shape unknown.</summary>
    [JsonPropertyName("lines")] public List<JsonElement> Lines { get; set; } = [];
}

/// <summary>Used by both reconstruction.hyphenation (with cases) and mainBody.hyphenation (without).</summary>
public sealed class HyphenationSummary
{
    [JsonPropertyName("lineBreakHyphens")] public int LineBreakHyphens { get; set; }
    [JsonPropertyName("joined")] public int Joined { get; set; }
    [JsonPropertyName("kept")] public int Kept { get; set; }
    [JsonPropertyName("uncertain")] public int Uncertain { get; set; }

    /// <summary>Present only under reconstruction.</summary>
    [JsonPropertyName("cases")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<HyphenationCase>? Cases { get; set; }
}

public sealed class HyphenationCase
{
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("blockId")] public int BlockId { get; set; }
    [JsonPropertyName("left")] public string Left { get; set; } = "";
    [JsonPropertyName("right")] public string Right { get; set; } = "";
    [JsonPropertyName("word")] public string Word { get; set; } = "";
    [JsonPropertyName("decision")] public string Decision { get; set; } = "";
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    [JsonPropertyName("uncertain")] public bool Uncertain { get; set; }
    [JsonPropertyName("mainBody")] public bool MainBody { get; set; }
}

#endregion

#region Main body

public sealed class MainBody
{
    [JsonPropertyName("paragraphs")] public List<MainBodyParagraph> Paragraphs { get; set; } = [];
    [JsonPropertyName("removed")] public RemovedContent Removed { get; set; } = new();
    [JsonPropertyName("headings")] public BlockWordCount Headings { get; set; } = new();
    [JsonPropertyName("script")] public ScriptSummary Script { get; set; } = new();
    [JsonPropertyName("runInLabels")] public List<RunInLabel> RunInLabels { get; set; } = [];
    [JsonPropertyName("firstLineIndents")] public FirstLineIndents FirstLineIndents { get; set; } = new();
    [JsonPropertyName("hyphenation")] public HyphenationSummary Hyphenation { get; set; } = new();
    [JsonPropertyName("citations")] public CitationDetail Citations { get; set; } = new();
    [JsonPropertyName("flags")] public List<string> Flags { get; set; } = [];
}

public sealed class MainBodyParagraph
{
    [JsonPropertyName("blockId")] public int BlockId { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("endPage")] public int EndPage { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = "";

    /// <summary>"paragraph" or "heading".</summary>
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";

    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("statements")] public int Statements { get; set; }
    [JsonPropertyName("runInLabels")] public List<string> RunInLabels { get; set; } = [];

    /// <summary>
    /// How the block counts toward Num_Paragraphs: "counted", "counted in parts", "heading",
    /// "list of short items", "no terminal punctuation", "credit line or sign-off", …
    /// </summary>
    [JsonPropertyName("paragraph")] public string ParagraphStatus { get; set; } = "";

    [JsonPropertyName("parts")] public List<ParagraphPart> Parts { get; set; } = [];
    [JsonPropertyName("paragraph3plus")] public bool Paragraph3Plus { get; set; }
    [JsonPropertyName("endsWithTerminalPunctuation")] public bool EndsWithTerminalPunctuation { get; set; }
    [JsonPropertyName("list")] public bool List { get; set; }
    [JsonPropertyName("crossesPageBreak")] public bool CrossesPageBreak { get; set; }
    [JsonPropertyName("pageStarts")] public List<PageStart> PageStarts { get; set; } = [];
    [JsonPropertyName("pageSplit")] public string PageSplit { get; set; } = "";
    [JsonPropertyName("rawText")] public string RawText { get; set; } = "";
}

public sealed class ParagraphPart
{
    /// <summary>"lead-in" or "item".</summary>
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("item")] public int Item { get; set; }
    [JsonPropertyName("char")] public int Char { get; set; }
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("statements")] public int Statements { get; set; }

    /// <summary>"counted" or "list of short items".</summary>
    [JsonPropertyName("paragraph")] public string ParagraphStatus { get; set; } = "";
}

public sealed class PageStart
{
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("char")] public int Char { get; set; }
}

public sealed class RemovedContent
{
    [JsonPropertyName("frontMatter")] public RemovedCount FrontMatter { get; set; } = new();
    [JsonPropertyName("backMatter")] public RemovedCount BackMatter { get; set; } = new();
    [JsonPropertyName("references")] public RemovedReferences References { get; set; } = new();
    [JsonPropertyName("noteEntries")] public RemovedCount NoteEntries { get; set; } = new();
    [JsonPropertyName("captions")] public RemovedCount Captions { get; set; } = new();
    [JsonPropertyName("figureText")] public RemovedCount FigureText { get; set; } = new();
    [JsonPropertyName("empty")] public RemovedCount Empty { get; set; } = new();
    [JsonPropertyName("citations")] public RemovedCitations Citations { get; set; } = new();
}

public class RemovedCount
{
    [JsonPropertyName("items")] public int Items { get; set; }
    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("chars")] public int Chars { get; set; }
}

public sealed class RemovedReferences : RemovedCount
{
    /// <summary>The reference heading text, or "n/a (no reference section found)".</summary>
    [JsonPropertyName("heading")] public string Heading { get; set; } = "";
}

public sealed class RemovedCitations
{
    [JsonPropertyName("authorYear")] public RemovedCount AuthorYear { get; set; } = new();
    [JsonPropertyName("bracketedNumbers")] public RemovedCount BracketedNumbers { get; set; } = new();
    [JsonPropertyName("parenthesisedNoteNumbers")] public RemovedCount ParenthesisedNoteNumbers { get; set; } = new();
    [JsonPropertyName("attachedNoteNumbers")] public RemovedCount AttachedNoteNumbers { get; set; } = new();
    [JsonPropertyName("superscriptNoteNumbers")] public RemovedCount SuperscriptNoteNumbers { get; set; } = new();
    [JsonPropertyName("backReferences")] public RemovedCount BackReferences { get; set; } = new();
    [JsonPropertyName("authorYearCitations")] public int AuthorYearCitations { get; set; }
    [JsonPropertyName("bracketedNumbersCited")] public int BracketedNumbersCited { get; set; }

    /// <summary>Sample removals keyed by category, e.g. "superscriptNoteNumbers" → ["(1)", "(2)"].</summary>
    [JsonPropertyName("examples")] public Dictionary<string, List<string>> Examples { get; set; } = [];
}

public sealed class BlockWordCount
{
    [JsonPropertyName("blocks")] public int Blocks { get; set; }
    [JsonPropertyName("words")] public int Words { get; set; }
}

public sealed class CountAndWords
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("words")] public int Words { get; set; }
}

public sealed class ScriptSummary
{
    [JsonPropertyName("letters")] public int Letters { get; set; }
    [JsonPropertyName("unsupportedLetters")] public int UnsupportedLetters { get; set; }
    [JsonPropertyName("unsupportedBlocks")] public int UnsupportedBlocks { get; set; }
    [JsonPropertyName("unsupportedDocument")] public bool UnsupportedDocument { get; set; }

    /// <summary>Unsupported script names ("CJK", "Arabic", …); empty in the sample.</summary>
    [JsonPropertyName("scripts")] public List<string> Scripts { get; set; } = [];
}

public sealed class RunInLabel
{
    [JsonPropertyName("blockId")] public int BlockId { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("alone")] public bool Alone { get; set; }
}

public sealed class FirstLineIndents
{
    [JsonPropertyName("blocks")] public int Blocks { get; set; }
    [JsonPropertyName("indented")] public int Indented { get; set; }
    [JsonPropertyName("documentIndents")] public bool DocumentIndents { get; set; }
}

public sealed class CitationDetail
{
    [JsonPropertyName("noteMarkers")] public List<int> NoteMarkers { get; set; } = [];
    [JsonPropertyName("noteEntries")] public List<int> NoteEntries { get; set; } = [];

    /// <summary>Always empty in the sample; element shape unknown.</summary>
    [JsonPropertyName("authorYear")] public List<JsonElement> AuthorYear { get; set; } = [];

    /// <summary>Always empty in the sample; element shape unknown.</summary>
    [JsonPropertyName("bracketed")] public List<JsonElement> Bracketed { get; set; } = [];

    [JsonPropertyName("backReferences")] public BackReferenceCounts BackReferences { get; set; } = new();
}

/// <summary>Used by mainBody.citations (no total) and core.Num_Citations (with total).</summary>
public sealed class BackReferenceCounts
{
    [JsonPropertyName("ibid")] public int Ibid { get; set; }
    [JsonPropertyName("opCit")] public int OpCit { get; set; }
    [JsonPropertyName("locCit")] public int LocCit { get; set; }

    [JsonPropertyName("total")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Total { get; set; }
}

#endregion

#region Core metrics

public sealed class CoreMetrics
{
    [JsonPropertyName("Num_Words")] public CountMetric NumWords { get; set; } = new();
    [JsonPropertyName("Num_Char")] public CharMetric NumChar { get; set; } = new();
    [JsonPropertyName("Num_Statements")] public CountMetric NumStatements { get; set; } = new();
    [JsonPropertyName("Num_Paragraphs")] public ParagraphsMetric NumParagraphs { get; set; } = new();
    [JsonPropertyName("Num_Paragraphs_3plus")] public Paragraphs3PlusMetric NumParagraphs3Plus { get; set; } = new();
    [JsonPropertyName("Num_Objects")] public ObjectsMetric NumObjects { get; set; } = new();
    [JsonPropertyName("Num_Citations")] public CitationsMetric NumCitations { get; set; } = new();
}

/// <summary>A count that becomes "n/a (unsupported script)" for unsupported-script documents.</summary>
public class CountMetric
{
    [JsonPropertyName("value")] public NaOr<int> Value { get; set; }
    [JsonPropertyName("note")] public string Note { get; set; } = "";
}

public sealed class CharMetric
{
    [JsonPropertyName("value")] public CharCounts Value { get; set; } = new();
    [JsonPropertyName("note")] public string Note { get; set; } = "";
}

public sealed class CharCounts
{
    [JsonPropertyName("withSpaces")] public int WithSpaces { get; set; }
    [JsonPropertyName("withoutSpaces")] public int WithoutSpaces { get; set; }
}

public sealed class ParagraphsMetric : CountMetric
{
    [JsonPropertyName("excluded")] public ParagraphExclusions Excluded { get; set; } = new();
}

public sealed class ParagraphExclusions
{
    [JsonPropertyName("headings")] public int Headings { get; set; }
    [JsonPropertyName("unsupportedScript")] public int UnsupportedScript { get; set; }
    [JsonPropertyName("runInLabels")] public int RunInLabels { get; set; }
    [JsonPropertyName("creditLinesAndSignOffs")] public int CreditLinesAndSignOffs { get; set; }
    [JsonPropertyName("listsOfShortItems")] public int ListsOfShortItems { get; set; }
    [JsonPropertyName("noTerminalPunctuation")] public int NoTerminalPunctuation { get; set; }
}

public sealed class Paragraphs3PlusMetric : CountMetric
{
    [JsonPropertyName("excluded")] public Paragraph3PlusExclusions Excluded { get; set; } = new();
}

public sealed class Paragraph3PlusExclusions
{
    [JsonPropertyName("twoOrFewerStatements")] public int TwoOrFewerStatements { get; set; }
    [JsonPropertyName("noTerminalPunctuation")] public int NoTerminalPunctuation { get; set; }
    [JsonPropertyName("headings")] public int Headings { get; set; }
    [JsonPropertyName("unsupportedScript")] public int UnsupportedScript { get; set; }
}

public sealed class ObjectsMetric
{
    [JsonPropertyName("value")] public int Value { get; set; }
    [JsonPropertyName("note")] public string Note { get; set; } = "";
    [JsonPropertyName("logos")] public int Logos { get; set; }
    [JsonPropertyName("images")] public int Images { get; set; }
    [JsonPropertyName("vectorFigures")] public int VectorFigures { get; set; }
    [JsonPropertyName("uncertain")] public int Uncertain { get; set; }
    [JsonPropertyName("ignoredBackgrounds")] public int IgnoredBackgrounds { get; set; }
    [JsonPropertyName("ignoredTiny")] public int IgnoredTiny { get; set; }
    [JsonPropertyName("ignoredVectorDecorations")] public int IgnoredVectorDecorations { get; set; }
    [JsonPropertyName("objects")] public List<DetectedObject> Objects { get; set; } = [];
}

public sealed class DetectedObject
{
    /// <summary>"raster" or "vector".</summary>
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("pages")] public List<int> Pages { get; set; } = [];

    /// <summary>[x0, y0, x1, y1] in points from the page's top-left corner.</summary>
    [JsonPropertyName("bbox")] public double[] Bbox { get; set; } = [];

    [JsonPropertyName("pxWidth")] public int PxWidth { get; set; }
    [JsonPropertyName("pxHeight")] public int PxHeight { get; set; }
    [JsonPropertyName("width")] public double Width { get; set; }
    [JsonPropertyName("height")] public double Height { get; set; }
    [JsonPropertyName("elements")] public int Elements { get; set; }
    [JsonPropertyName("caption")] public string Caption { get; set; } = "";
    [JsonPropertyName("uncertain")] public bool Uncertain { get; set; }

    /// <summary>"image", "logo", "vector figure", "decoration", …</summary>
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("counted")] public bool Counted { get; set; }
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
}

public sealed class CitationsMetric
{
    [JsonPropertyName("value")] public int Value { get; set; }
    [JsonPropertyName("note")] public string Note { get; set; } = "";
    [JsonPropertyName("numberedNotes")] public int NumberedNotes { get; set; }
    [JsonPropertyName("authorYear")] public int AuthorYear { get; set; }
    [JsonPropertyName("bracketed")] public int Bracketed { get; set; }
    [JsonPropertyName("backReferences")] public BackReferenceCounts BackReferences { get; set; } = new();
}

#endregion

#region Supporting measures

public sealed class Measures
{
    [JsonPropertyName("vocabulary")] public Measure<VocabularyValue> Vocabulary { get; set; } = new();
    [JsonPropertyName("readability")] public Measure<ReadabilityValue> Readability { get; set; } = new();
    [JsonPropertyName("sentenceLength")] public Measure<SentenceLengthValue> SentenceLength { get; set; } = new();
    [JsonPropertyName("language")] public Measure<LanguageValue> Language { get; set; } = new();
    [JsonPropertyName("markers")] public Measure<MarkersValue> Markers { get; set; } = new();
    [JsonPropertyName("structure")] public Measure<StructureValue> Structure { get; set; } = new();
    [JsonPropertyName("pages")] public Measure<PagesValue> Pages { get; set; } = new();
    [JsonPropertyName("search")] public NoteOnly Search { get; set; } = new();
}

/// <summary>A supporting measure; its value is "n/a (unsupported script)" for unsupported-script documents.</summary>
public sealed class Measure<T>
{
    [JsonPropertyName("value")] public NaOr<T> Value { get; set; }
    [JsonPropertyName("note")] public string Note { get; set; } = "";
}

public sealed class NoteOnly
{
    [JsonPropertyName("note")] public string Note { get; set; } = "";
}

public sealed class VocabularyValue
{
    [JsonPropertyName("tokens")] public int Tokens { get; set; }
    [JsonPropertyName("types")] public int Types { get; set; }
    [JsonPropertyName("hapaxLegomena")] public int HapaxLegomena { get; set; }
    [JsonPropertyName("typeTokenRatio")] public double TypeTokenRatio { get; set; }
    [JsonPropertyName("meanCharactersPerWord")] public double MeanCharactersPerWord { get; set; }
    [JsonPropertyName("meanSyllablesPerWord")] public double MeanSyllablesPerWord { get; set; }
    [JsonPropertyName("shareLongerThan6")] public double ShareLongerThan6 { get; set; }
}

public sealed class ReadabilityValue
{
    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("sentences")] public int Sentences { get; set; }
    [JsonPropertyName("syllables")] public int Syllables { get; set; }
    [JsonPropertyName("complexWords")] public int ComplexWords { get; set; }
    [JsonPropertyName("letters")] public int Letters { get; set; }
    [JsonPropertyName("wordsOutsideStatements")] public int WordsOutsideStatements { get; set; }
    [JsonPropertyName("fleschReadingEase")] public NaOr<double> FleschReadingEase { get; set; }
    [JsonPropertyName("fleschKincaidGrade")] public NaOr<double> FleschKincaidGrade { get; set; }
    [JsonPropertyName("gunningFog")] public NaOr<double> GunningFog { get; set; }
    [JsonPropertyName("colemanLiau")] public NaOr<double> ColemanLiau { get; set; }
    [JsonPropertyName("readingTimeMinutes")] public double ReadingTimeMinutes { get; set; }
    [JsonPropertyName("wordsPerMinute")] public int WordsPerMinute { get; set; }
}

public sealed class SentenceLengthValue
{
    [JsonPropertyName("sentences")] public int Sentences { get; set; }
    [JsonPropertyName("shortest")] public NaOr<double> Shortest { get; set; }
    [JsonPropertyName("median")] public NaOr<double> Median { get; set; }
    [JsonPropertyName("mean")] public NaOr<double> Mean { get; set; }
    [JsonPropertyName("longest")] public NaOr<double> Longest { get; set; }
    [JsonPropertyName("bands")] public List<LengthBand> Bands { get; set; } = [];

    /// <summary>Words per statement, in reading order.</summary>
    [JsonPropertyName("lengths")] public List<int> Lengths { get; set; } = [];

    [JsonPropertyName("longestSentence")] public NaOr<LongestSentence> LongestSentence { get; set; }
}

public sealed class LengthBand
{
    [JsonPropertyName("band")] public string Band { get; set; } = "";
    [JsonPropertyName("from")] public int From { get; set; }

    /// <summary>Upper bound; "n/a (no upper limit)" for the last band.</summary>
    [JsonPropertyName("to")] public NaOr<int> To { get; set; }

    [JsonPropertyName("count")] public int Count { get; set; }
}

public sealed class LongestSentence
{
    [JsonPropertyName("text")] public string Text { get; set; } = "";
    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("pages")] public List<int> Pages { get; set; } = [];
}

public sealed class LanguageValue
{
    [JsonPropertyName("topTerms")] public List<TermCount> TopTerms { get; set; } = [];
    [JsonPropertyName("topContentTerms")] public List<TermCount> TopContentTerms { get; set; } = [];
    [JsonPropertyName("repeatedBigrams")] public List<TermCount> RepeatedBigrams { get; set; } = [];
    [JsonPropertyName("repeatedContentBigrams")] public List<TermCount> RepeatedContentBigrams { get; set; } = [];
    [JsonPropertyName("distinctRepeatedBigrams")] public int DistinctRepeatedBigrams { get; set; }
    [JsonPropertyName("wordLengths")] public List<WordLengthCount> WordLengths { get; set; } = [];
    [JsonPropertyName("functionWordList")] public FunctionWordList FunctionWordList { get; set; } = new();
}

public sealed class TermCount
{
    [JsonPropertyName("term")] public string Term { get; set; } = "";
    [JsonPropertyName("count")] public int Count { get; set; }
}

public sealed class WordLengthCount
{
    /// <summary>"1" … "14", then "15+".</summary>
    [JsonPropertyName("length")] public string Length { get; set; } = "";
    [JsonPropertyName("count")] public int Count { get; set; }
}

public sealed class FunctionWordList
{
    [JsonPropertyName("size")] public int Size { get; set; }
}

public sealed class MarkersValue
{
    [JsonPropertyName("questions")] public int Questions { get; set; }
    [JsonPropertyName("exclamations")] public int Exclamations { get; set; }
    [JsonPropertyName("quotedPassages")] public int QuotedPassages { get; set; }
    [JsonPropertyName("unmatchedQuoteMarks")] public int UnmatchedQuoteMarks { get; set; }
    [JsonPropertyName("authorYearCitations")] public int AuthorYearCitations { get; set; }
    [JsonPropertyName("authorYearGroups")] public int AuthorYearGroups { get; set; }
    [JsonPropertyName("bracketedReferences")] public int BracketedReferences { get; set; }
    [JsonPropertyName("bracketedNumbersCited")] public int BracketedNumbersCited { get; set; }
    [JsonPropertyName("numberedNoteMarkers")] public int NumberedNoteMarkers { get; set; }
    [JsonPropertyName("urls")] public int Urls { get; set; }
    [JsonPropertyName("dois")] public int Dois { get; set; }
    [JsonPropertyName("urlsAndDois")] public int UrlsAndDois { get; set; }
    [JsonPropertyName("numerals")] public int Numerals { get; set; }
    [JsonPropertyName("percentages")] public int Percentages { get; set; }
}

public sealed class StructureValue
{
    [JsonPropertyName("paragraphs")] public ParagraphStats Paragraphs { get; set; } = new();
    [JsonPropertyName("headings")] public CountAndWords Headings { get; set; } = new();
    [JsonPropertyName("exclusions")] public ExclusionCounts Exclusions { get; set; } = new();
}

public sealed class ParagraphStats
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("meanWords")] public NaOr<double> MeanWords { get; set; }
    [JsonPropertyName("medianWords")] public NaOr<double> MedianWords { get; set; }
    [JsonPropertyName("meanStatements")] public NaOr<double> MeanStatements { get; set; }
    [JsonPropertyName("medianStatements")] public NaOr<double> MedianStatements { get; set; }
    [JsonPropertyName("longest")] public NaOr<LongestParagraph> Longest { get; set; }
}

public sealed class LongestParagraph
{
    [JsonPropertyName("blockId")] public int BlockId { get; set; }
    [JsonPropertyName("pages")] public List<int> Pages { get; set; } = [];
    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("statements")] public int Statements { get; set; }
    [JsonPropertyName("excerpt")] public string Excerpt { get; set; } = "";
}

public sealed class ExclusionCounts
{
    [JsonPropertyName("frontMatterBlocks")] public int FrontMatterBlocks { get; set; }
    [JsonPropertyName("frontMatterWords")] public int FrontMatterWords { get; set; }
    [JsonPropertyName("backMatterBlocks")] public int BackMatterBlocks { get; set; }
    [JsonPropertyName("backMatterWords")] public int BackMatterWords { get; set; }
    [JsonPropertyName("referenceSectionBlocks")] public int ReferenceSectionBlocks { get; set; }
    [JsonPropertyName("referenceSectionWords")] public int ReferenceSectionWords { get; set; }
    [JsonPropertyName("noteEntryBlocks")] public int NoteEntryBlocks { get; set; }
    [JsonPropertyName("noteEntryWords")] public int NoteEntryWords { get; set; }
    [JsonPropertyName("captionBlocks")] public int CaptionBlocks { get; set; }
    [JsonPropertyName("captionWords")] public int CaptionWords { get; set; }
    [JsonPropertyName("figureTextBlocks")] public int FigureTextBlocks { get; set; }
    [JsonPropertyName("figureTextWords")] public int FigureTextWords { get; set; }
    [JsonPropertyName("emptyBlocks")] public int EmptyBlocks { get; set; }
    [JsonPropertyName("authorYearGroupsRemoved")] public int AuthorYearGroupsRemoved { get; set; }
    [JsonPropertyName("bracketedGroupsRemoved")] public int BracketedGroupsRemoved { get; set; }
    [JsonPropertyName("parenthesisedNoteNumbersRemoved")] public int ParenthesisedNoteNumbersRemoved { get; set; }
    [JsonPropertyName("attachedNoteNumbersRemoved")] public int AttachedNoteNumbersRemoved { get; set; }
    [JsonPropertyName("superscriptNoteNumbersRemoved")] public int SuperscriptNoteNumbersRemoved { get; set; }
    [JsonPropertyName("backReferencesRemoved")] public int BackReferencesRemoved { get; set; }
    [JsonPropertyName("headingBlocks")] public int HeadingBlocks { get; set; }
    [JsonPropertyName("headingWords")] public int HeadingWords { get; set; }
    [JsonPropertyName("runInLabels")] public int RunInLabels { get; set; }
    [JsonPropertyName("paragraphsLeftOutRunInLabelsAlone")] public int ParagraphsLeftOutRunInLabelsAlone { get; set; }
    [JsonPropertyName("paragraphsLeftOutCreditLinesAndSignOffs")] public int ParagraphsLeftOutCreditLinesAndSignOffs { get; set; }
    [JsonPropertyName("paragraphsLeftOutListsOfShortItems")] public int ParagraphsLeftOutListsOfShortItems { get; set; }
    [JsonPropertyName("paragraphsLeftOutWithoutTerminalPunctuation")] public int ParagraphsLeftOutWithoutTerminalPunctuation { get; set; }
    [JsonPropertyName("unsupportedScriptBlocks")] public int UnsupportedScriptBlocks { get; set; }
    [JsonPropertyName("paragraphs3plusLeftOutTwoOrFewerStatements")] public int Paragraphs3PlusLeftOutTwoOrFewerStatements { get; set; }
    [JsonPropertyName("paragraphs3plusLeftOutWithoutTerminalPunctuation")] public int Paragraphs3PlusLeftOutWithoutTerminalPunctuation { get; set; }
    [JsonPropertyName("runningHeadersRemoved")] public int RunningHeadersRemoved { get; set; }
    [JsonPropertyName("runningFootersRemoved")] public int RunningFootersRemoved { get; set; }
    [JsonPropertyName("pageNumbersRemoved")] public int PageNumbersRemoved { get; set; }
    [JsonPropertyName("ambiguousPageJoins")] public int AmbiguousPageJoins { get; set; }
    [JsonPropertyName("pageAndColumnJoinsWithoutIndent")] public int PageAndColumnJoinsWithoutIndent { get; set; }
    [JsonPropertyName("ignoredBackgroundImages")] public int IgnoredBackgroundImages { get; set; }
    [JsonPropertyName("ignoredSpacerImages")] public int IgnoredSpacerImages { get; set; }
    [JsonPropertyName("ignoredVectorDecorations")] public int IgnoredVectorDecorations { get; set; }
    [JsonPropertyName("uncertainObjects")] public int UncertainObjects { get; set; }
    [JsonPropertyName("lineBreakHyphensJoined")] public int LineBreakHyphensJoined { get; set; }
    [JsonPropertyName("lineBreakHyphensKept")] public int LineBreakHyphensKept { get; set; }
    [JsonPropertyName("uncertainHyphens")] public int UncertainHyphens { get; set; }
    [JsonPropertyName("flags")] public int Flags { get; set; }
}

public sealed class PagesValue
{
    [JsonPropertyName("rows")] public List<PageRow> Rows { get; set; } = [];
}

public sealed class PageRow
{
    [JsonPropertyName("page")] public int Page { get; set; }

    /// <summary>Printed page number, or "n/a (no printed page number found)".</summary>
    [JsonPropertyName("label")] public string Label { get; set; } = "";

    [JsonPropertyName("characters")] public int Characters { get; set; }
    [JsonPropertyName("words")] public int Words { get; set; }
    [JsonPropertyName("sentences")] public int Sentences { get; set; }

    /// <summary>"n/a (no statement ends on this page)" when no statement ends on the page.</summary>
    [JsonPropertyName("wordsPerSentence")] public NaOr<double> WordsPerSentence { get; set; }

    [JsonPropertyName("shareOfWords")] public NaOr<double> ShareOfWords { get; set; }
}

#endregion