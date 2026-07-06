namespace Tuvima.Wikidata;

/// <summary>
/// Count metadata discovered for a manifest container independently from the
/// concrete child rows currently available from Wikidata traversal.
/// </summary>
public sealed class ManifestCountFact
{
    /// <summary>
    /// The total-count domain, such as "manifest_items", "issues", "volumes",
    /// "chapters", "seasons", or "tracks".
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>The expected total count for <see cref="Kind"/>.</summary>
    public int Count { get; init; }

    /// <summary>Where the count was sourced from.</summary>
    public string? Source { get; init; }

    /// <summary>Confidence from 0.0 to 1.0.</summary>
    public double Confidence { get; init; }

    /// <summary>Optional short explanation for consumers and diagnostics.</summary>
    public string? Note { get; init; }
}
