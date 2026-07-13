namespace Tuvima.Wikidata;

/// <summary>
/// Broad media classification for one Wikidata series-manifest member.
/// The value is derived from the member's own entity evidence rather than
/// inherited from the requested series container.
/// </summary>
public enum SeriesManifestMediaKind
{
    Unknown = 0,
    LiteraryWork = 1,
    Audiobook = 2,
    Film = 3,
    Television = 4,
    Comic = 5,
    Music = 6,
    StageWork = 7,
    VideoGame = 8
}
