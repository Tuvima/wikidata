namespace Tuvima.Wikidata.Internal;

/// <summary>Accumulates diagnostics for one bridge request across resolution phases.</summary>
internal sealed class BridgeDiagnosticsBuilder
{
    public List<string> AttemptedStrategies { get; } = [];
    public List<string> MatchedProperties { get; } = [];
    public List<string> RejectedCandidates { get; } = [];
    public List<string> Warnings { get; } = [];
    public int DistinctLookupCount { get; set; }
    public int FetchedEntityCount { get; set; }
    public string? CompletedPhase { get; set; }

    public BridgeResolutionDiagnostics Build(
        TimeSpan providerLatency,
        WikidataDiagnosticsSnapshot before,
        WikidataDiagnosticsSnapshot after)
    {
        return new BridgeResolutionDiagnostics
        {
            AttemptedStrategies = AttemptedStrategies.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            MatchedProperties = MatchedProperties.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            RejectedCandidates = RejectedCandidates.ToList(),
            Warnings = Warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ProviderLatency = providerLatency,
            CacheHits = Math.Max(0, after.CacheHits - before.CacheHits),
            CacheMisses = Math.Max(0, after.CacheMisses - before.CacheMisses),
            RetryCount = Math.Max(0, after.RetryCount - before.RetryCount),
            RateLimitResponses = Math.Max(0, after.RateLimitResponses - before.RateLimitResponses),
            DistinctLookupCount = DistinctLookupCount,
            FetchedEntityCount = FetchedEntityCount,
            Elapsed = providerLatency,
            CompletedPhase = CompletedPhase
        };
    }
}
