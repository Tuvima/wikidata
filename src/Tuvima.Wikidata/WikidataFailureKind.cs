namespace Tuvima.Wikidata;

/// <summary>
/// Typed provider failure categories reported by the HTTP pipeline and diagnostics.
/// </summary>
public enum WikidataFailureKind
{
    NotFound,
    NoSitelink,
    RateLimited,
    TransientNetworkFailure,
    MalformedResponse,
    Cancelled,
    /// <summary>The provider rejected a request with a non-retryable API error.</summary>
    ProviderRejected
}
