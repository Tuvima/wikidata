namespace Tuvima.Wikidata;

/// <summary>
/// Structural scope of an item relative to the requested series container.
/// This keeps the primary sequence separate from supplemental works and
/// collection expansion without relying on title or QID allowlists.
/// </summary>
public enum SeriesManifestItemScope
{
    MainSequence = 0,
    Supplementary = 1,
    CollectedContent = 2,
    BroaderContext = 3,
    Unpositioned = 4
}
