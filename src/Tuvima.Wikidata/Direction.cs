namespace Tuvima.Wikidata;

/// <summary>
/// Indicates the direction of a relationship relative to a given entity.
/// Used by child entity traversal and the graph module.
/// </summary>
public enum Direction
{
    /// <summary>
    /// The entity is the subject (source) of the relationship.
    /// For traversal: follow the property forward from the parent to its children.
    /// </summary>
    Outgoing,

    /// <summary>
    /// The entity is the object (target) of the relationship.
    /// For traversal: find entities whose property points to the parent.
    /// </summary>
    Incoming
}
