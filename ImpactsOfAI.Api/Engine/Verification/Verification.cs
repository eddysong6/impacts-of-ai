// Requires .NET 6+ (file-scoped namespaces, records).

using System.Text;

namespace ImpactsOfAI.Api.Engine.Verification;

public enum MatchKind { Exact, Fuzzy, None }

/// <param name="Kind">Exact = verbatim after normalization; Fuzzy = close enough to flag as lower confidence.</param>
/// <param name="Similarity">0..1. 1.0 for exact matches.</param>
/// <param name="StartOffset">Offset into the ORIGINAL source (-1 if no match).</param>
/// <param name="Length">Length of the matched region in the ORIGINAL source.</param>
/// <param name="MatchedText">What the source actually says at that location (useful for diffing against the excerpt).</param>
public sealed record ExcerptMatch(
    MatchKind Kind, double Similarity, int StartOffset, int Length, string? MatchedText);

/// <summary>Normalized text plus a map from each normalized char back to its index in the original.</summary>
public sealed record NormalizedText(string Text, int[] OriginalIndex);

public static class TextNormalizer
{
    public static NormalizedText Normalize(string source)
    {
        var sb = new StringBuilder(source.Length);
        var map = new List<int>(source.Length);
        bool lastWasSpace = true; // also trims leading whitespace

        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];

            // Rejoin words hyphenated across line breaks ("exam-\nple" -> "example").
            if (c == '-' && TryGetHyphenatedBreakEnd(source, i, out int resumeAt))
            {
                i = resumeAt - 1;
                continue;
            }

            // Drop invisible characters that PDF extraction tends to leave behind.
            if (c == '\u00AD' || c == '\u200B' || c == '\uFEFF') continue;

            // Collapse all whitespace runs to a single space.
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    map.Add(i);
                    lastWasSpace = true;
                }
                continue;
            }
            lastWasSpace = false;

            // Unify typographic quotes and dashes.
            c = c switch
            {
                '\u2018' or '\u2019' or '\u201B' => '\'',
                '\u201C' or '\u201D' => '"',
                '\u2013' or '\u2014' => '-',
                _ => c
            };

            // NFKC per char (expands ligatures like "ﬁ" -> "fi"). Lone surrogates would
            // make Normalize() throw, so pass them through untouched.
            string expanded = char.IsSurrogate(c) ? c.ToString() : c.ToString().Normalize(NormalizationForm.FormKC);
            foreach (char e in expanded)
            {
                sb.Append(char.ToLowerInvariant(e));
                map.Add(i); // every expanded char maps back to the same original char
            }
        }

        // Trim trailing space.
        if (sb.Length > 0 && sb[^1] == ' ')
        {
            sb.Length--;
            map.RemoveAt(map.Count - 1);
        }

        return new NormalizedText(sb.ToString(), map.ToArray());
    }

    // True if source[i] is a hyphen between two letters with a line break after it.
    private static bool TryGetHyphenatedBreakEnd(string s, int i, out int resumeAt)
    {
        resumeAt = i;
        if (i == 0 || !char.IsLetter(s[i - 1])) return false;

        int j = i + 1;
        while (j < s.Length && (s[j] == ' ' || s[j] == '\t')) j++;
        if (j >= s.Length || (s[j] != '\n' && s[j] != '\r')) return false;

        while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
        if (j >= s.Length || !char.IsLetter(s[j])) return false;

        resumeAt = j;
        return true;
    }
}

public sealed class ExcerptVerifier
{
    private readonly string _source;
    private readonly NormalizedText _normalized;
    private readonly double _threshold;
    private readonly int _minFuzzyLength;

    /// <param name="threshold">Minimum similarity (0..1) to accept a fuzzy match. 0.90 is a sensible start.</param>
    /// <param name="minFuzzyLength">
    /// Excerpts shorter than this (normalized chars) must match exactly. Short strings
    /// produce false-positive fuzzy matches because a few edits is a large fraction of them.
    /// </param>
    public ExcerptVerifier(string source, double threshold = 0.90, int minFuzzyLength = 25)
    {
        _source = source;
        _normalized = TextNormalizer.Normalize(source); // normalize once, verify many findings
        _threshold = threshold;
        _minFuzzyLength = minFuzzyLength;
    }

    public ExcerptMatch Verify(string excerpt)
    {
        // Remove outer quotes.
        if (excerpt.Length > 0 &&
            excerpt[0] is '\u201C' or '\u201D' or '"' &&
            excerpt[^1] is '\u201C' or '\u201D' or '"')
        {
            excerpt = excerpt[1..^1];
        }
        
        string pattern = TextNormalizer.Normalize(excerpt).Text;
        if (pattern.Length == 0) return NoMatch(0);

        // 1. Exact (post-normalization) match: cheap, and the common case.
        int exactIdx = _normalized.Text.IndexOf(pattern, StringComparison.Ordinal);
        if (exactIdx >= 0)
            return Build(MatchKind.Exact, 1.0, exactIdx, exactIdx + pattern.Length);

        // 2. Fuzzy fallback.
        if (pattern.Length < _minFuzzyLength) return NoMatch(0);

        var (distance, start, end) = BestApproximateMatch(_normalized.Text, pattern);
        double similarity = 1.0 - (double)distance / pattern.Length;

        return similarity >= _threshold
            ? Build(MatchKind.Fuzzy, similarity, start, end)
            : NoMatch(similarity);
    }

    /// <summary>
    /// Sellers' algorithm: edit distance between <paramref name="pattern"/> and the best-matching
    /// SUBSTRING of <paramref name="text"/> (free start and end in the text).
    /// Returns (distance, start, end) where the match is text[start..end).
    /// Time O(n*m), memory O(m). Roughly 60M cell updates for a 200k-char source and a
    /// 300-char excerpt, which is fast enough to run per finding.
    /// </summary>
    private static (int Distance, int Start, int End) BestApproximateMatch(string text, string pattern)
    {
        int n = text.Length, m = pattern.Length;

        // Row i = pattern prefix length. Each cell tracks its cost and where the match began.
        var prevCost = new int[m + 1];
        var prevStart = new int[m + 1];
        var currCost = new int[m + 1];
        var currStart = new int[m + 1];

        for (int i = 0; i <= m; i++) { prevCost[i] = i; prevStart[i] = 0; }

        int bestCost = int.MaxValue, bestStart = 0, bestEnd = 0;

        for (int j = 1; j <= n; j++)
        {
            currCost[0] = 0;   // free start: an empty pattern prefix matches anywhere
            currStart[0] = j;

            char tc = text[j - 1];
            for (int i = 1; i <= m; i++)
            {
                int subCost = prevCost[i - 1] + (pattern[i - 1] == tc ? 0 : 1); // match / substitute
                int skipText = prevCost[i] + 1;                                 // extra char in source
                int skipPat = currCost[i - 1] + 1;                              // char missing from source

                if (subCost <= skipText && subCost <= skipPat)
                { currCost[i] = subCost; currStart[i] = prevStart[i - 1]; }
                else if (skipText <= skipPat)
                { currCost[i] = skipText; currStart[i] = prevStart[i]; }
                else
                { currCost[i] = skipPat; currStart[i] = currStart[i - 1]; }
            }

            if (currCost[m] < bestCost)
            {
                bestCost = currCost[m];
                bestStart = currStart[m];
                bestEnd = j;
                if (bestCost == 0) break;
            }

            (prevCost, currCost) = (currCost, prevCost);
            (prevStart, currStart) = (currStart, prevStart);
        }

        return (bestCost, bestStart, bestEnd);
    }

    // Convert a [start, end) range in normalized text back to the original source.
    private ExcerptMatch Build(MatchKind kind, double similarity, int normStart, int normEnd)
    {
        int origStart = _normalized.OriginalIndex[normStart];
        int origEnd = _normalized.OriginalIndex[normEnd - 1] + 1;
        return new ExcerptMatch(
            kind, similarity, origStart, origEnd - origStart,
            _source.Substring(origStart, origEnd - origStart));
    }

    private static ExcerptMatch NoMatch(double bestSimilarity) =>
        new(MatchKind.None, bestSimilarity, -1, 0, null);
}

/* ---------------------------------------------------------------------------
   Example usage, mapping results to the verification_status from the pipeline:

   var verifier = new ExcerptVerifier(sourceText);

   foreach (var finding in findings)
   {
       var match = verifier.Verify(finding.EvidenceExcerpt);

       finding.VerificationStatus = match.Kind switch
       {
           MatchKind.Exact => "verified",
           MatchKind.Fuzzy => "flagged",   // real text, but not verbatim: lower confidence
           _               => "dropped"    // or retry Stage 1 with this failure fed back
       };

       finding.EvidenceOffset = match.StartOffset; // for highlighting in the UI
   }
--------------------------------------------------------------------------- */