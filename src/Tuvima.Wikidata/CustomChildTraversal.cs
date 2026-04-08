namespace Tuvima.Wikidata;

/// <summary>
/// Escape-hatch descriptor for <see cref="ChildEntityKind.Custom"/> traversals.
/// Lets callers discover children using arbitrary property paths without a library update.
/// </summary>
public sealed class CustomChildTraversal
{
    /// <summary>The Wikidata property to traverse (e.g., "P527" for "has parts").</summary>
    public required string RelationshipProperty { get; init; }

    /// <summary>
    /// Direction of traversal. <see cref="Direction.Outgoing"/> follows the property forward
    /// from the parent; <see cref="Direction.Incoming"/> finds entities whose property points to the parent.
    /// </summary>
    public Direction Direction { get; init; } = Direction.Outgoing;

    /// <summary>
    /// Optional P31 type filter. Only children whose P31 includes one of these QIDs are kept.
    /// </summary>
    public IReadOnlyList<string>? ChildTypeFilter { get; init; }

    /// <summary>
    /// Property used for ordering children. Defaults to "P1545" (series ordinal).
    /// </summary>
    public string OrdinalProperty { get; init; } = "P1545";

    /// <summary>
    /// Optional creator roles to populate on each <see cref="ChildEntityRef"/>, keyed by role name
    /// (e.g., "Director") with property ID (e.g., "P57") as value.
    /// </summary>
    public IReadOnlyDictionary<string, string>? CreatorRoles { get; init; }
}
