namespace Tuvima.Wikidata;

/// <summary>
/// Progress event emitted by long-running Wikidata operations.
/// </summary>
public sealed record WikidataProgressEvent(
    string Operation,
    string Phase,
    string? CorrelationKey,
    int CompletedItems,
    int TotalItems,
    int CompletedWorkUnits,
    int TotalWorkUnits,
    TimeSpan Elapsed,
    string? Message,
    WikidataFailureKind? FailureKind);

public static class WikidataProgressOperations
{
    public const string BridgeResolution = "bridge_resolution";
}

public static class WikidataProgressPhases
{
    public const string Planned = "planned";
    public const string ExternalIdLookup = "external_id_lookup";
    public const string EntityFetch = "entity_fetch";
    public const string TextFallback = "text_fallback";
    public const string Rollup = "rollup";
    public const string RelationshipExtract = "relationship_extract";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
