namespace Tuvima.Wikidata;

/// <summary>
/// Coarse classification for an external grouping/container entity before it is
/// used as a media shelf, broader rollup, or diagnostic source.
/// </summary>
public enum WikidataContainerKind
{
    Unknown = 0,
    OrderedSeries = 1,
    Franchise = 2,
    Universe = 3,
    WikimediaList = 4,
    PublisherOrProductionList = 5,
    AlbumRelease = 6,
    TvShow = 7,
    TvSeason = 8,
    ComicSeries = 9,
    MangaSeries = 10
}
