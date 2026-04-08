namespace Tuvima.Wikidata;

/// <summary>
/// Stage 2 resolve request for generic text-based reconciliation. Reconciles the title
/// against a type-filtered search (via CirrusSearch) with an optional author constraint.
/// </summary>
public sealed class TextStage2Request : IStage2Request
{
    /// <inheritdoc />
    public required string CorrelationKey { get; init; }

    /// <summary>The title / primary text to reconcile.</summary>
    public required string Title { get; init; }

    /// <summary>Optional author / creator — scored as a P50 (author) constraint when set.</summary>
    public string? Author { get; init; }

    /// <summary>
    /// CirrusSearch type QIDs. The library requires this to be non-empty by default
    /// (the "strict no-unfiltered-text" rule). To explicitly opt out, set
    /// <see cref="AllowUnfilteredText"/> to true.
    /// </summary>
    public required IReadOnlyList<string> CirrusSearchTypes { get; init; }

    /// <summary>
    /// When true, an empty <see cref="CirrusSearchTypes"/> list is allowed and the query
    /// runs without a type filter. Use only when the consumer has deliberately decided
    /// that type filtering should be skipped for this request. Default false.
    /// </summary>
    public bool AllowUnfilteredText { get; init; }

    /// <summary>Optional pipeline of query cleaners, e.g. <see cref="QueryCleaners"/>.</summary>
    public IReadOnlyList<Func<string, string>>? QueryCleaners { get; init; }

    /// <summary>Language for search and label resolution. Defaults to the configured language.</summary>
    public string? Language { get; init; }

    /// <summary>
    /// Minimum normalized score (0.0–1.0) for the text match to be considered resolved.
    /// Default 0.70.
    /// </summary>
    public double AcceptThreshold { get; init; } = 0.70;
}
