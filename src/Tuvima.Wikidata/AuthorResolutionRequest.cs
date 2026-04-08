namespace Tuvima.Wikidata;

/// <summary>
/// A request to resolve one or more author names to Wikidata author entities,
/// handling multi-author strings and pen-name detection.
/// </summary>
public sealed class AuthorResolutionRequest
{
    /// <summary>
    /// The raw author string as it appears in source data.
    /// Multi-author strings separated by "and", "&amp;", ";", ",", CJK commas, or "with" will be split.
    /// Names in "Last, First" form (detected heuristically) are not split.
    /// Trailing "et al." markers are captured in <see cref="AuthorResolutionResult.UnresolvedNames"/>.
    /// </summary>
    public required string RawAuthorString { get; init; }

    /// <summary>
    /// Optional QID of the associated work. When set, it's used as a context hint
    /// during reconciliation to prefer authors who have this work on their bibliography.
    /// </summary>
    public string? WorkQidHint { get; init; }

    /// <summary>
    /// Language for labels and search. Defaults to the reconciler's configured language.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// When true (default), detected authors are checked for P742 (pseudonym) claims
    /// and their real-name QID (resolved via the pseudonym's owner) is populated on
    /// <see cref="ResolvedAuthor.RealNameQid"/>.
    /// </summary>
    public bool DetectPseudonyms { get; init; } = true;
}
