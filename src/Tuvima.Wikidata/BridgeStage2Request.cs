namespace Tuvima.Wikidata;

/// <summary>
/// Stage 2 resolve request driven by external identifiers (ISBN, IMDB, MusicBrainz,
/// OpenLibrary, etc.). The resolver walks the <see cref="BridgeIds"/> dictionary in
/// preferred order, calling <c>LookupByExternalIdAsync</c> for each known identifier
/// until one resolves, then optionally pivots to a work via <see cref="EditionPivot"/>.
/// </summary>
public sealed class BridgeStage2Request : IStage2Request
{
    /// <inheritdoc />
    public required string CorrelationKey { get; init; }

    /// <summary>
    /// The source data's bridge identifiers, keyed by source-specific name
    /// (e.g. <c>{"isbn13": "9780441172719", "openlibrary": "OL24229316M"}</c>).
    /// Sentinel / placeholder keys whose values are empty strings are skipped automatically.
    /// </summary>
    public required IReadOnlyDictionary<string, string> BridgeIds { get; init; }

    /// <summary>
    /// Maps each key in <see cref="BridgeIds"/> to the Wikidata property ID that holds
    /// that identifier (e.g. <c>{"isbn13": "P212", "openlibrary": "P648"}</c>).
    /// Keys not present in this map are ignored during resolution.
    /// </summary>
    public required IReadOnlyDictionary<string, string> WikidataProperties { get; init; }

    /// <summary>
    /// Preferred resolution order. Keys are tried in this order; the first successful
    /// lookup wins. When null, keys are tried in the order they appear in <see cref="BridgeIds"/>.
    /// </summary>
    public IReadOnlyList<string>? PreferredOrder { get; init; }

    /// <summary>
    /// When set, the resolver will attempt to pivot from the resolved entity to its parent
    /// work (or to a preferred edition). See <see cref="EditionPivotRule"/>.
    /// </summary>
    public EditionPivotRule? EditionPivot { get; init; }

    /// <summary>Language for label resolution on the final result. Defaults to the configured language.</summary>
    public string? Language { get; init; }
}
