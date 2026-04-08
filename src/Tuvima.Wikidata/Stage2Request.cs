namespace Tuvima.Wikidata;

/// <summary>
/// Static factory for constructing <see cref="IStage2Request"/> instances dynamically.
/// Useful when the resolution strategy depends on runtime data rather than a compile-time decision.
/// </summary>
public static class Stage2Request
{
    /// <summary>
    /// Constructs a <see cref="BridgeStage2Request"/> from bridge IDs and their Wikidata property map.
    /// </summary>
    public static BridgeStage2Request Bridge(
        string correlationKey,
        IReadOnlyDictionary<string, string> bridgeIds,
        IReadOnlyDictionary<string, string> wikidataProperties,
        EditionPivotRule? editionPivot = null,
        IReadOnlyList<string>? preferredOrder = null,
        string? language = null)
        => new()
        {
            CorrelationKey = correlationKey,
            BridgeIds = bridgeIds,
            WikidataProperties = wikidataProperties,
            PreferredOrder = preferredOrder,
            EditionPivot = editionPivot,
            Language = language
        };

    /// <summary>
    /// Constructs a <see cref="MusicStage2Request"/>.
    /// </summary>
    public static MusicStage2Request Music(
        string correlationKey,
        string albumTitle,
        string? artist = null,
        string? language = null)
        => new()
        {
            CorrelationKey = correlationKey,
            AlbumTitle = albumTitle,
            Artist = artist,
            Language = language
        };

    /// <summary>
    /// Constructs a <see cref="TextStage2Request"/>.
    /// </summary>
    public static TextStage2Request Text(
        string correlationKey,
        string title,
        IReadOnlyList<string> cirrusSearchTypes,
        string? author = null,
        IReadOnlyList<Func<string, string>>? queryCleaners = null,
        string? language = null,
        double acceptThreshold = 0.70)
        => new()
        {
            CorrelationKey = correlationKey,
            Title = title,
            CirrusSearchTypes = cirrusSearchTypes,
            Author = author,
            QueryCleaners = queryCleaners,
            Language = language,
            AcceptThreshold = acceptThreshold
        };
}
