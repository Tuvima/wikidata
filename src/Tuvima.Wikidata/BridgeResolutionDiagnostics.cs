namespace Tuvima.Wikidata;

/// <summary>
/// Explainability details produced while resolving one bridge request.
/// </summary>
public sealed class BridgeResolutionDiagnostics
{
    public IReadOnlyList<string> AttemptedStrategies { get; init; } = [];

    public IReadOnlyList<string> MatchedProperties { get; init; } = [];

    public IReadOnlyList<string> RejectedCandidates { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public TimeSpan ProviderLatency { get; init; }

    public long CacheHits { get; init; }

    public long CacheMisses { get; init; }

    public long RetryCount { get; init; }

    public long RateLimitResponses { get; init; }

    public int DistinctLookupCount { get; init; }

    public int FetchedEntityCount { get; init; }

    public TimeSpan Elapsed { get; init; }

    public string? CompletedPhase { get; init; }
}
