namespace Tuvima.Wikidata;

/// <summary>
/// A section from a Wikipedia article's table of contents.
/// Use <see cref="WikidataReconciler.GetWikipediaSectionContentAsync"/> with the <see cref="Index"/> to fetch the section's content.
/// </summary>
public sealed class WikipediaSection
{
    /// <summary>
    /// The section heading text (e.g., "Plot summary", "Early life and education").
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The section index for use with <see cref="WikidataReconciler.GetWikipediaSectionContentAsync"/>.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// The HTML heading level (2 = h2/top-level section, 3 = h3/subsection, etc.).
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// The table-of-contents number (e.g., "1", "1.1", "2.3").
    /// </summary>
    public string Number { get; init; } = "";

    /// <summary>
    /// The URL anchor for direct linking (e.g., "Plot_summary").
    /// </summary>
    public string Anchor { get; init; } = "";
}
