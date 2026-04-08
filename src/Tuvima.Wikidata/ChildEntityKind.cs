namespace Tuvima.Wikidata;

/// <summary>
/// Preset child-traversal strategies for <see cref="ChildEntityRequest"/>.
/// </summary>
public enum ChildEntityKind
{
    /// <summary>
    /// TV show → seasons (P527, filtered to Q3464665) → episodes (P527, filtered to Q21191270).
    /// Primary count = seasons, total count = seasons + episodes.
    /// </summary>
    TvSeasonsAndEpisodes = 1,

    /// <summary>
    /// Album → tracks (P658 with P527 fallback). Returns ordered track list.
    /// </summary>
    MusicTracks = 2,

    /// <summary>
    /// Comic series → issues via reverse P179 (part of the series) filtered to Q14406742.
    /// </summary>
    ComicIssues = 3,

    /// <summary>
    /// Book → sequels via P156 (followed by) and P155 (follows).
    /// </summary>
    BookSequels = 4,

    /// <summary>
    /// Custom traversal specified by <see cref="ChildEntityRequest.CustomTraversal"/>.
    /// </summary>
    Custom = 99
}
