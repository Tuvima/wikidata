namespace Tuvima.Wikidata;

/// <summary>
/// Stage 2 resolve request for music albums. The resolver reconciles the album title
/// against Q482994 (music album) with an optional artist constraint (P175).
/// </summary>
public sealed class MusicStage2Request : IStage2Request
{
    /// <inheritdoc />
    public required string CorrelationKey { get; init; }

    /// <summary>The album title to reconcile.</summary>
    public required string AlbumTitle { get; init; }

    /// <summary>Optional artist name — scored as a P175 (performer) constraint when set.</summary>
    public string? Artist { get; init; }

    /// <summary>Language for search and label resolution. Defaults to the configured language.</summary>
    public string? Language { get; init; }
}
