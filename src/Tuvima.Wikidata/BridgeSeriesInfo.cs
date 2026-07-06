namespace Tuvima.Wikidata;

/// <summary>
/// Normalized series/order metadata extracted from Wikidata claims.
/// </summary>
public sealed class BridgeSeriesInfo
{
    public string? SeriesQid { get; init; }

    public string? SeriesLabel { get; init; }

    /// <summary>The classified kind of the referenced external container.</summary>
    public WikidataContainerKind ContainerKind { get; init; } = WikidataContainerKind.Unknown;

    /// <summary>
    /// True when the referenced container can be used as an immediate ordered/lane shelf.
    /// Broader franchises, universes, and Wikimedia lists are returned for diagnostics but
    /// should not be promoted into <c>series_qid</c>.
    /// </summary>
    public bool IsImmediateSeries { get; init; }

    public string? Position { get; init; }

    public string? PreviousQid { get; init; }

    public string? NextQid { get; init; }

    public string? SourcePropertyId { get; init; }

    public double Confidence { get; init; }
}
