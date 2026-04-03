namespace Tuvima.Wikidata.Graph;

/// <summary>
/// A directed relationship edge between two entities in the graph.
/// </summary>
public sealed class GraphEdge
{
    /// <summary>Source entity QID.</summary>
    public required string SubjectQid { get; init; }

    /// <summary>Edge type (e.g. "father", "member_of").</summary>
    public required string Relationship { get; init; }

    /// <summary>Target entity QID.</summary>
    public required string ObjectQid { get; init; }

    /// <summary>Optional confidence weight. Default is 1.0.</summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>Optional QID of the work providing context for this relationship.</summary>
    public string? ContextWorkQid { get; init; }
}
