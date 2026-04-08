namespace Tuvima.Wikidata;

/// <summary>
/// A request to build a structured <see cref="ChildEntityManifest"/> for a parent entity.
/// Use one of the preset <see cref="ChildEntityKind"/> values for common cases,
/// or <see cref="ChildEntityKind.Custom"/> with a <see cref="CustomTraversal"/> for arbitrary traversals.
/// </summary>
public sealed class ChildEntityRequest
{
    /// <summary>The parent entity QID (e.g., "Q3577037" for Breaking Bad).</summary>
    public required string ParentQid { get; init; }

    /// <summary>The preset that controls which traversal and child shape to use.</summary>
    public required ChildEntityKind Kind { get; init; }

    /// <summary>Language for labels and monolingual text. Defaults to the reconciler's configured language.</summary>
    public string? Language { get; init; }

    /// <summary>
    /// Maximum number of "primary" children (e.g., seasons for a TV show).
    /// Applied after sorting by ordinal then release date.
    /// </summary>
    public int MaxPrimary { get; init; } = 20;

    /// <summary>
    /// Maximum number of total children in the manifest, summed across all levels
    /// (e.g., seasons + episodes).
    /// </summary>
    public int MaxTotal { get; init; } = 500;

    /// <summary>
    /// When true, fetches and populates <see cref="ChildEntityRef.Creators"/> with
    /// role → name pairs for directors, writers, performers, etc.
    /// </summary>
    public bool IncludeCreatorProperties { get; init; } = true;

    /// <summary>
    /// Required when <see cref="Kind"/> is <see cref="ChildEntityKind.Custom"/>. Ignored otherwise.
    /// </summary>
    public CustomChildTraversal? CustomTraversal { get; init; }
}
