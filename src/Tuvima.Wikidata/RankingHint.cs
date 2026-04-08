namespace Tuvima.Wikidata;

/// <summary>
/// A soft ranking signal used by Stage 2 edition pivoting to pick between multiple candidates.
/// Each hint identifies a property on candidate entities and one or more string values to fuzzy-match
/// against that property's claim values (or their resolved entity labels for entity-valued properties).
/// </summary>
public sealed class RankingHint
{
    /// <summary>The Wikidata property ID to read on each candidate (e.g. <c>"P175"</c> for performer).</summary>
    public required string PropertyId { get; init; }

    /// <summary>
    /// One or more fuzzy-match targets. For entity-valued properties (wikibase-item), the hint is
    /// compared against the resolved entity label. For string properties, it is compared against
    /// the raw value. The candidate's score is the best fuzzy match across all hints.
    /// </summary>
    public required IReadOnlyList<string> Values { get; init; }

    /// <summary>
    /// Relative weight of this hint when multiple hints are present on the same rule. Default 1.0.
    /// </summary>
    public double Weight { get; init; } = 1.0;
}
