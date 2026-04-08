namespace Tuvima.Wikidata;

/// <summary>
/// Identifies which resolution strategy produced a <see cref="Stage2Result"/>.
/// Set by the library — callers do not supply this value.
/// </summary>
public enum Stage2MatchedStrategy
{
    /// <summary>No strategy succeeded.</summary>
    NotResolved = 0,

    /// <summary>Resolved by external-identifier lookup (<see cref="BridgeStage2Request"/>).</summary>
    BridgeId = 1,

    /// <summary>Resolved by music album reconciliation (<see cref="MusicStage2Request"/>).</summary>
    MusicAlbum = 2,

    /// <summary>Resolved by text reconciliation with type filter (<see cref="TextStage2Request"/>).</summary>
    TextReconciliation = 3
}
