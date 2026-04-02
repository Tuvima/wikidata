namespace Tuvima.WikidataReconciliation;

/// <summary>
/// The content of a single Wikipedia section or subsection.
/// </summary>
public sealed class SectionContent
{
    /// <summary>
    /// The section heading text (e.g., "Plot", "Season 1").
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The section body text with the heading stripped.
    /// </summary>
    public required string Content { get; init; }
}
