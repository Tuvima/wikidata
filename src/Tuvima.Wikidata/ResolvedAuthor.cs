namespace Tuvima.Wikidata;

/// <summary>
/// A single author extracted from a multi-author string and resolved against Wikidata.
/// </summary>
public sealed class ResolvedAuthor
{
    /// <summary>
    /// The name as it appeared in the input, after splitting and trimming.
    /// </summary>
    public required string OriginalName { get; init; }

    /// <summary>
    /// The resolved Wikidata QID, or null if reconciliation failed.
    /// </summary>
    public string? Qid { get; init; }

    /// <summary>
    /// The canonical display label for the resolved entity, or null if unresolved.
    /// </summary>
    public string? CanonicalName { get; init; }

    /// <summary>
    /// For solo pen names resolved via reverse P742 lookup (Pattern 1): the QID of the real
    /// author. Populated when the input string was a pen name like "Richard Bachman" and
    /// <see cref="Services.AuthorsService.ResolveAsync"/> found a Wikidata entity whose P742
    /// claim matches the input. In that case <see cref="Qid"/> is also set to this QID —
    /// there is no separate entity for the pen name itself. Null for non-pseudonym lookups,
    /// for collective pseudonyms (use <see cref="RealAuthors"/> instead), and when pseudonym
    /// detection is disabled.
    /// </summary>
    public string? RealNameQid { get; init; }

    /// <summary>
    /// For collective pseudonyms (Pattern 3): the real authors the pseudonym represents.
    /// Populated when the resolved entity has a P31 instance-of one of the pseudonym classes
    /// (collective pseudonym Q16017119, pen name Q4647632, etc.) and Wikidata lists the real
    /// authors via P50 (author), P170 (creator), or related properties. Example: looking up
    /// "James S.A. Corey" resolves to the collective pseudonym entity and populates this with
    /// Daniel Abraham and Ty Franck. Null for solo authors, unresolved inputs, and when
    /// pseudonym detection is disabled.
    /// </summary>
    /// <remarks>
    /// Entries in this list are lightweight <see cref="RealAuthor"/> refs rather than full
    /// <see cref="ResolvedAuthor"/> objects — the library does not recursively expand nested
    /// pseudonyms. If a real author discovered through collective-pseudonym expansion is itself
    /// a pseudonym (rare), that inner layer is not resolved.
    /// </remarks>
    public IReadOnlyList<RealAuthor>? RealAuthors { get; init; }

    /// <summary>
    /// The resolved entity's own P742 (pseudonym) claims as raw strings, when the author
    /// uses one or more pen names. Populated by <see cref="Services.AuthorsService.ResolveAsync"/>
    /// when <see cref="AuthorResolutionRequest.DetectPseudonyms"/> is true. Null otherwise.
    /// </summary>
    /// <remarks>
    /// Wikidata typically models solo pen names as P742 string values on the real author's
    /// entity rather than as separate entities. If Stephen King (Q39829) has P742 = "Richard
    /// Bachman", looking up "Stephen King" will resolve to Q39829 and populate this list with
    /// "Richard Bachman". This is distinct from <see cref="RealAuthors"/>, which handles the
    /// collective-pseudonym case where the pseudonym has its own entity.
    /// </remarks>
    public IReadOnlyList<string>? Pseudonyms { get; init; }

    /// <summary>
    /// The reconciliation score (0.0–100.0) that led to this match. Zero when unresolved.
    /// </summary>
    public double Confidence { get; init; }
}
