namespace CVRanker.Domain;

/// <summary>
/// v1 scorer lifted from prototype/scorer-v1.html:
/// Score = 100 x sum(w*f) / sum(w), BM25-lite (k1=1.2, b=0.75, no IDF)
/// normalized intra-offer by max, soft veto (must=0 tailed, never rejected).
/// </summary>
public sealed class Bm25Ranker(double k1 = 1.2, double b = 0.75) : IRanker
{
    public const string ScorerVersion = "scorer-v1";

    public IReadOnlyList<RankedCandidate> Rank(Offer offer, IEnumerable<CandidateCv> cvs)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(cvs);

        var list = cvs.ToList();
        if (list.Count == 0)
            return [];

        var weights = offer.EffectiveWeights;
        var must = NormalizeEntries(offer.MustHave);
        var nice = NormalizeEntries(offer.NiceToHave);

        var queryTokens = TextTokenizer.Tokenize(offer.JobDescription);
        var queryTerms = new HashSet<string>(queryTokens, StringComparer.Ordinal);
        var docTokens = list.Select(cv => TextTokenizer.Tokenize(cv.Text)).ToList();
        var avgLen = docTokens.Average(t => t.Count == 0 ? 1 : t.Count);

        var rawBm25 = list.Select((cv, i) => RawBm25(queryTerms, docTokens[i], avgLen)).ToList();
        var maxBm25 = rawBm25.Max();
        if (maxBm25 <= 0) maxBm25 = 1;

        var scored = new List<RankedCandidate>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var cv = list[i];
            var docSet = new HashSet<string>(docTokens[i], StringComparer.Ordinal);

            var mustHit = must.Where(m => m.IsCoveredBy(docSet)).ToList();
            var niceHit = nice.Where(n => n.IsCoveredBy(docSet)).ToList();
            var missing = must.Where(m => !m.IsCoveredBy(docSet)).Select(m => m.Display).ToList();

            double mustF = must.Count == 0 ? 0 : (double)mustHit.Count / must.Count;
            double niceF = nice.Count == 0 ? 0 : (double)niceHit.Count / nice.Count;

            var breakdown = new FeatureBreakdown(
                Must: mustF,
                Bm25: rawBm25[i] / maxBm25,
                Nice: niceF,
                Experience: Math.Clamp(cv.ExperienceYears / 10.0, 0, 1.0),
                Education: cv.HasDegree ? 1 : 0,
                Title: cv.IsSenior ? 1 : 0,
                Languages: cv.HasLanguage ? 1 : 0);

            double weighted = weights.WeightedSum(breakdown);
            double wsum = weights.Sum;
            double score = wsum <= 0 ? 0 : 100.0 * weighted / wsum;

            var reasons = mustHit.Select(h => $"must:{h.Display}")
                .Concat(niceHit.Select(h => $"nice:{h.Display}"))
                .Append($"exp:{TrimYears(cv.ExperienceYears)}y")
                .Append(cv.HasDegree ? "degree" : "no-degree")
                .Take(3)
                .ToList();

            scored.Add(new RankedCandidate(cv.Id, Rank: 0, Score: score, TopReasons: reasons, MissingMust: missing, Breakdown: breakdown));
        }

        // Soft veto: zero must-coverage tails the group (when a must list exists), never auto-reject.
        bool vetoActive = must.Count > 0;
        var ordered = scored
            .OrderByDescending(r => vetoActive && r.Breakdown.Must > 0)
            .ThenByDescending(r => r.Score)
            .Select((r, idx) => r with { Rank = idx + 1 })
            .ToList();

        return ordered;
    }

    private double RawBm25(HashSet<string> queryTerms, List<string> doc, double avgLen)
    {
        if (queryTerms.Count == 0 || doc.Count == 0)
            return 0;
        var tf = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var t in doc)
            tf[t] = tf.TryGetValue(t, out var c) ? c + 1 : 1;
        double dl = doc.Count;
        double s = 0;
        foreach (var term in queryTerms)
        {
            if (!tf.TryGetValue(term, out var f) || f == 0)
                continue;
            s += f * (k1 + 1) / (f + k1 * (1 - b + b * dl / avgLen));
        }
        return s;
    }

    // Single normalization path for Offer entries: same tokenizer as CV text,
    // plus trailing-period trim so HR-typed "C#." matches CV term "c#".
    // Doc side keeps raw tokenizer output (prototype scorer-v1 fidelity).
    private static List<EntryTerms> NormalizeEntries(IEnumerable<string>? items) =>
        (items ?? [])
            .Select(item => EntryTerms.From(item))
            .Where(e => e.Tokens.Count > 0)
            .ToList();

    private sealed record EntryTerms(string Display, IReadOnlyList<string> Tokens)
    {
        public bool IsCoveredBy(HashSet<string> docSet) => Tokens.All(docSet.Contains);

        public static EntryTerms From(string item)
        {
            var tokens = TextTokenizer.Tokenize(item)
                .Select(t => t.TrimEnd('.'))
                .Where(t => t.Length > 0)
                .ToList();
            return new EntryTerms(string.Join(' ', tokens), tokens);
        }
    }

    private static string TrimYears(double years) =>
        Math.Abs(years - Math.Round(years)) < 1e-9 ? ((int)Math.Round(years)).ToString() : years.ToString("0.#");
}
