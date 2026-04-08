namespace Tuvima.Wikidata;

/// <summary>
/// A single child entity entry in a <see cref="ChildEntityManifest"/>.
/// </summary>
public sealed class ChildEntityRef
{
    /// <summary>The child's Wikidata QID.</summary>
    public required string Qid { get; init; }

    /// <summary>The child's label in the requested language.</summary>
    public string? Title { get; init; }

    /// <summary>
    /// Position within the parent (P1545 series ordinal). For episodes this is the episode number,
    /// for tracks the track number, for comic issues the issue number.
    /// </summary>
    public int? Ordinal { get; init; }

    /// <summary>
    /// Parent position for multi-level manifests. For TV episodes this is the season number;
    /// for multi-disc albums this is the disc number. Null at the top level.
    /// </summary>
    public int? Parent { get; init; }

    /// <summary>Publication date parsed from P577 if available.</summary>
    public DateOnly? ReleaseDate { get; init; }

    /// <summary>Duration parsed from P2047 if available (typically for episodes / tracks).</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Creator role → display name map (e.g. {"Director": "Vince Gilligan", "Writer": "..."})
    /// when the request asked for creator properties.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Creators { get; init; }
}
