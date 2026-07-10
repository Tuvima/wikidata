namespace Tuvima.Wikidata;

/// <summary>
/// A work or collection row in a Wikidata series manifest.
/// </summary>
public sealed class SeriesManifestItem
{
    public required string Qid { get; init; }
    public string? Label { get; init; }
    public string? Description { get; init; }
    public string? RawSeriesOrdinal { get; init; }
    public decimal? ParsedSeriesOrdinal { get; init; }
    /// <summary>
    /// QID of the container whose ordinal is represented by
    /// <see cref="RawSeriesOrdinal"/>. This prevents an anthology position from
    /// being displayed as the position in the broader series.
    /// </summary>
    public string? OrdinalScopeQid { get; init; }
    public DateOnly? PublicationDate { get; init; }
    public string? PreviousQid { get; init; }
    public string? NextQid { get; init; }
    public string? ParentCollectionQid { get; init; }
    public string? ParentCollectionLabel { get; init; }
    public bool IsCollection { get; init; }
    public bool IsExpandedFromCollection { get; init; }
    /// <summary>
    /// Structural membership scope derived from the Wikidata relationship path.
    /// P179 and direct P527 members are the main sequence; direct P361 members
    /// are supplementary; expanded P527 children are collected content.
    /// </summary>
    public SeriesManifestItemScope MembershipScope { get; init; } = SeriesManifestItemScope.MainSequence;
    public IReadOnlyList<string> SourceProperties { get; init; } = [];
    public SeriesManifestOrderSource OrderSource { get; init; }
    public IReadOnlyList<SeriesManifestRelationship> Relationships { get; init; } = [];
}
