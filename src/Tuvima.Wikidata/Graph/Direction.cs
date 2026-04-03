namespace Tuvima.Wikidata.Graph;

/// <summary>
/// Indicates the direction of a relationship relative to a given entity.
/// </summary>
public enum Direction
{
    /// <summary>The entity is the subject (source) of the relationship.</summary>
    Outgoing,

    /// <summary>The entity is the object (target) of the relationship.</summary>
    Incoming
}
