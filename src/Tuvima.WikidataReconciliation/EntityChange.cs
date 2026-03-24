namespace Tuvima.WikidataReconciliation;

/// <summary>
/// Represents a change to a Wikidata entity from the Recent Changes feed.
/// </summary>
public sealed class EntityChange
{
    /// <summary>
    /// The entity ID that was changed (e.g., "Q42").
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// The type of change: "edit", "new", "log".
    /// </summary>
    public required string ChangeType { get; init; }

    /// <summary>
    /// When the change occurred (UTC).
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The user who made the change.
    /// </summary>
    public string? User { get; init; }

    /// <summary>
    /// Edit summary/comment.
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// The revision ID after the change.
    /// </summary>
    public long RevisionId { get; init; }
}
