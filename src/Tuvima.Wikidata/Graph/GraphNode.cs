namespace Tuvima.Wikidata.Graph;

/// <summary>
/// An entity node in the graph. Represents a Wikidata entity with optional metadata.
/// </summary>
public sealed class GraphNode
{
    /// <summary>Wikidata QID (e.g. "Q937618").</summary>
    public required string Qid { get; init; }

    /// <summary>Human-readable label.</summary>
    public string? Label { get; init; }

    /// <summary>Discriminator for the entity type (e.g. "Character", "Location").</summary>
    public string? Type { get; init; }

    /// <summary>QIDs of works this entity appears in. Used by <see cref="EntityGraph.FindCrossMediaEntities"/>.</summary>
    public IReadOnlyList<string>? WorkQids { get; init; }
}
