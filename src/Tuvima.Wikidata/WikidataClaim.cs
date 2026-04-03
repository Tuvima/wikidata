namespace Tuvima.Wikidata;

/// <summary>
/// A claim (statement) from a Wikidata entity, including its qualifiers.
/// </summary>
public sealed class WikidataClaim
{
    /// <summary>
    /// The property ID (e.g., "P31" for instance of).
    /// </summary>
    public required string PropertyId { get; init; }

    /// <summary>
    /// The claim rank: "preferred", "normal", or "deprecated".
    /// </summary>
    public required string Rank { get; init; }

    /// <summary>
    /// The main value of the claim. Null for novalue/somevalue snaks.
    /// </summary>
    public WikidataValue? Value { get; init; }

    /// <summary>
    /// Qualifiers keyed by property ID. Each qualifier property may have multiple values.
    /// For example, P580 (start time) qualifiers on a P39 (position held) claim.
    /// Empty dictionary if no qualifiers.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<WikidataValue>> Qualifiers { get; init; }
        = new Dictionary<string, IReadOnlyList<WikidataValue>>();

    /// <summary>
    /// Ordered list of qualifier property IDs, preserving Wikidata's display order.
    /// Empty if no qualifiers.
    /// </summary>
    public IReadOnlyList<string> QualifierOrder { get; init; } = [];
}
