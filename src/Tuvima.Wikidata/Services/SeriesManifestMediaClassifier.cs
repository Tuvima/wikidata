namespace Tuvima.Wikidata.Services;

internal static class SeriesManifestMediaClassifier
{
    private static readonly HashSet<string> StageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q25379",       // play
        "Q43099500",    // theatrical production
        "Q17537576"     // creative work for the stage
    };

    private static readonly HashSet<string> AudiobookTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q106833"
    };

    private static readonly HashSet<string> FilmTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q11424",       // film
        "Q506240"       // television film
    };

    private static readonly HashSet<string> TelevisionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q5398426",     // television series
        "Q21191270",    // television series episode
        "Q3464665"      // television season
    };

    private static readonly HashSet<string> ComicTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q1004",        // comic book
        "Q8274",        // manga
        "Q3297186",     // comic book limited series
        "Q21198342"     // manga series
    };

    private static readonly HashSet<string> MusicTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q482994",      // album
        "Q2031291",     // release
        "Q208569",      // studio album
        "Q7366"         // song
    };

    private static readonly HashSet<string> VideoGameTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q7889"
    };

    private static readonly HashSet<string> LiteraryTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q571",         // book
        "Q8261",        // novel
        "Q149537",      // novella
        "Q49084",       // short story
        "Q7725634",     // literary work
        "Q47461344"     // written work
    };

    public static SeriesManifestMediaKind Classify(
        IReadOnlyCollection<string> instanceOfQids,
        string? description)
    {
        if (instanceOfQids.Any(StageTypes.Contains))
            return SeriesManifestMediaKind.StageWork;
        if (instanceOfQids.Any(AudiobookTypes.Contains))
            return SeriesManifestMediaKind.Audiobook;
        if (instanceOfQids.Any(FilmTypes.Contains))
            return SeriesManifestMediaKind.Film;
        if (instanceOfQids.Any(TelevisionTypes.Contains))
            return SeriesManifestMediaKind.Television;
        if (instanceOfQids.Any(ComicTypes.Contains))
            return SeriesManifestMediaKind.Comic;
        if (instanceOfQids.Any(MusicTypes.Contains))
            return SeriesManifestMediaKind.Music;
        if (instanceOfQids.Any(VideoGameTypes.Contains))
            return SeriesManifestMediaKind.VideoGame;
        if (instanceOfQids.Any(LiteraryTypes.Contains))
            return SeriesManifestMediaKind.LiteraryWork;

        var text = description?.Trim().ToLowerInvariant() ?? "";
        if (text.Contains("stage play", StringComparison.Ordinal)
            || text.Contains("theatrical production", StringComparison.Ordinal)
            || text.EndsWith(" play", StringComparison.Ordinal)
            || text.StartsWith("play ", StringComparison.Ordinal))
        {
            return SeriesManifestMediaKind.StageWork;
        }

        if (text.Contains("audiobook", StringComparison.Ordinal)
            || text.Contains("audio book", StringComparison.Ordinal))
        {
            return SeriesManifestMediaKind.Audiobook;
        }

        if (text.Contains("novel", StringComparison.Ordinal)
            || text.Contains("book", StringComparison.Ordinal)
            || text.Contains("literary work", StringComparison.Ordinal)
            || text.Contains("written work", StringComparison.Ordinal))
        {
            return SeriesManifestMediaKind.LiteraryWork;
        }

        if (text.Contains("film", StringComparison.Ordinal) || text.Contains("movie", StringComparison.Ordinal))
            return SeriesManifestMediaKind.Film;
        if (text.Contains("television", StringComparison.Ordinal) || text.Contains("tv episode", StringComparison.Ordinal))
            return SeriesManifestMediaKind.Television;
        if (text.Contains("comic", StringComparison.Ordinal) || text.Contains("manga", StringComparison.Ordinal))
            return SeriesManifestMediaKind.Comic;
        if (text.Contains("album", StringComparison.Ordinal) || text.Contains("song", StringComparison.Ordinal))
            return SeriesManifestMediaKind.Music;
        if (text.Contains("video game", StringComparison.Ordinal))
            return SeriesManifestMediaKind.VideoGame;

        return SeriesManifestMediaKind.Unknown;
    }
}
