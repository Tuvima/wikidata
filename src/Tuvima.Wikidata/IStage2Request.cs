namespace Tuvima.Wikidata;

/// <summary>
/// Marker interface for all Stage 2 resolve requests. The library ships three concrete
/// implementations: <see cref="BridgeStage2Request"/>, <see cref="MusicStage2Request"/>,
/// and <see cref="TextStage2Request"/>. Custom implementations are not supported — the
/// library's batch grouping logic uses an exhaustive switch over the three known types.
/// <para>
/// Callers that need to construct requests dynamically from heterogeneous source data
/// can use the static factory methods on <see cref="Stage2Request"/>.
/// </para>
/// </summary>
public interface IStage2Request
{
    /// <summary>
    /// Caller-supplied key that identifies this request in a batch. The key is used as the
    /// dictionary key in the response from <see cref="Services.Stage2Service.ResolveBatchAsync"/>.
    /// Must be unique within a single batch.
    /// </summary>
    string CorrelationKey { get; }
}
