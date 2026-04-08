namespace Tuvima.Wikidata;

/// <summary>
/// Describes how a Stage 2 bridge resolution should pivot between work-level and edition-level
/// entities after the initial lookup. Used by <see cref="BridgeStage2Request.EditionPivot"/>.
/// <para>
/// Media-type-agnostic: callers configure work vs edition classes and (optionally) a ranking
/// hint for picking the best edition among multiple matches.
/// </para>
/// </summary>
public sealed class EditionPivotRule
{
    /// <summary>
    /// P31 QIDs that identify work-level instances (e.g. Q7725634 "literary work").
    /// When the resolved entity matches one of these, no pivot is performed.
    /// </summary>
    public IReadOnlyList<string> WorkClasses { get; init; } = [];

    /// <summary>
    /// P31 QIDs that identify edition-level instances (e.g. Q3331189 "version, edition, or translation",
    /// Q122731938 "audiobook edition").
    /// When the resolved entity matches one of these, the resolver walks P629 (edition of) to find the work.
    /// When the resolved entity is a work but the caller wants a specific edition, the resolver walks
    /// P747 (has edition) and ranks results using <see cref="RankingHints"/>.
    /// </summary>
    public IReadOnlyList<string> EditionClasses { get; init; } = [];

    /// <summary>
    /// When true, prefer pivoting a work to its preferred edition (via P747 + ranking) rather than
    /// staying on the work. When false, edition → work pivoting still happens but work → edition does not.
    /// Default false.
    /// </summary>
    public bool PreferEdition { get; init; }

    /// <summary>
    /// Optional ranking hints used when <see cref="PreferEdition"/> is true and multiple editions
    /// match the work. Editions are scored against each hint and the best match wins.
    /// </summary>
    public IReadOnlyList<RankingHint>? RankingHints { get; init; }
}
