namespace Tuvima.Wikidata;

/// <summary>
/// Information about a child entity discovered via a relationship property.
/// For example, a TV episode within a season, a track within an album, or a book within a series.
/// </summary>
public sealed class ChildEntityInfo
{
    /// <summary>
    /// The Wikidata entity ID of the child (e.g., "Q65090726").
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// The label of the child entity in the requested language.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// The description of the child entity.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// P1545 (series ordinal) parsed as an integer, if present on the child entity.
    /// Used for ordering (e.g., episode number, track number).
    /// </summary>
    public int? Ordinal { get; init; }

    /// <summary>
    /// The requested properties fetched for this child entity.
    /// Keys are property IDs (e.g., "P1476"), values are the claims for that property.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<WikidataClaim>> Properties { get; init; }
        = new Dictionary<string, IReadOnlyList<WikidataClaim>>();
}
