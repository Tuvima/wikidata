namespace Tuvima.Wikidata;

/// <summary>
/// A structured manifest of children discovered for a parent entity.
/// Returned by <see cref="Services.ChildrenService.GetChildEntitiesAsync"/>.
/// </summary>
public sealed class ChildEntityManifest
{
    /// <summary>The parent QID the manifest was built for.</summary>
    public required string ParentQid { get; init; }

    /// <summary>
    /// The number of primary children found (before any cap was applied).
    /// For <see cref="ChildEntityKind.TvSeasonsAndEpisodes"/> this is the season count.
    /// For other presets this equals <see cref="TotalCount"/>.
    /// </summary>
    public int PrimaryCount { get; init; }

    /// <summary>
    /// The total number of children in the manifest, across all levels.
    /// For TV shows this is seasons + episodes.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The children themselves, in traversal order (sorted by ordinal then release date).
    /// </summary>
    public IReadOnlyList<ChildEntityRef> Children { get; init; } = [];
}
