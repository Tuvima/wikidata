namespace Tuvima.Wikidata;

/// <summary>
/// The outcome of a Stage 2 resolve request.
/// </summary>
public sealed class Stage2Result
{
    /// <summary>True when a QID was resolved (regardless of strategy).</summary>
    public bool Found { get; init; }

    /// <summary>
    /// The resolved entity QID. For bridge/music requests this is the first successful lookup.
    /// For bridge requests with edition pivoting, this is the final entity after pivoting
    /// (i.e., the work or preferred edition).
    /// </summary>
    public string? Qid { get; init; }

    /// <summary>
    /// For bridge requests that pivoted to a work: the work's QID. Otherwise the same as <see cref="Qid"/>
    /// for work-level resolutions, or null for edition-level resolutions that did not pivot.
    /// </summary>
    public string? WorkQid { get; init; }

    /// <summary>
    /// For bridge requests that resolved to an edition: the edition's QID. Null when the
    /// resolved entity was already a work, or when no edition pivoting was requested.
    /// </summary>
    public string? EditionQid { get; init; }

    /// <summary>
    /// True when the resolved <see cref="Qid"/> is an edition (P31 includes one of the
    /// <see cref="EditionPivotRule.EditionClasses"/>).
    /// </summary>
    public bool IsEdition { get; init; }

    /// <summary>Which strategy produced this result.</summary>
    public Stage2MatchedStrategy MatchedBy { get; init; }

    /// <summary>
    /// For bridge resolutions: the identifier key from <see cref="BridgeStage2Request.BridgeIds"/>
    /// that produced the match (e.g. <c>"isbn13"</c>). Null for other strategies.
    /// </summary>
    public string? PrimaryBridgeIdType { get; init; }

    /// <summary>
    /// For bridge resolutions: the subset of <see cref="BridgeStage2Request.BridgeIds"/> whose
    /// corresponding Wikidata property was also present on the resolved entity. Useful for
    /// propagating validated identifiers to downstream storage.
    /// </summary>
    public IReadOnlyDictionary<string, string> CollectedBridgeIds { get; init; }
        = new Dictionary<string, string>();

    /// <summary>
    /// The display label of the resolved entity in the requested language, when available.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>A singleton representing "nothing was resolved." Useful as a default return value.</summary>
    public static Stage2Result NotFound { get; } = new()
    {
        Found = false,
        MatchedBy = Stage2MatchedStrategy.NotResolved
    };
}
