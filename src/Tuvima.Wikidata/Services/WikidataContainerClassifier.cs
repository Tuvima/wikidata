namespace Tuvima.Wikidata.Services;

internal static class WikidataContainerClassifier
{
    private const string InstanceOf = "P31";

    private static readonly HashSet<string> WikimediaListTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q13406463"
    };

    private static readonly HashSet<string> FranchiseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q196600"
    };

    private static readonly HashSet<string> AlbumTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q482994",
        "Q2031291",
        "Q208569",
        "Q108346082"
    };

    private static readonly HashSet<string> TvShowTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q5398426"
    };

    private static readonly HashSet<string> TvSeasonTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q3464665"
    };

    private static readonly HashSet<string> ComicSeriesTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q1004",
        "Q3297186"
    };

    private static readonly HashSet<string> MangaSeriesTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q21198342"
    };

    private static readonly HashSet<string> OrderedSeriesTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Q24856",
        "Q7725310",
        "Q277759"
    };

    public static WikidataContainerKind Classify(WikidataEntityInfo? entity)
    {
        if (entity is null)
            return WikidataContainerKind.Unknown;

        return Classify(entity.Label, entity.Description, GetEntityIds(entity, InstanceOf));
    }

    public static WikidataContainerKind Classify(
        string? label,
        string? description,
        IReadOnlyCollection<string>? typeIds)
    {
        IReadOnlyCollection<string> types = typeIds ?? Array.Empty<string>();
        var text = $"{label} {description}".Trim().ToLowerInvariant();
        var labelText = label?.Trim().ToLowerInvariant() ?? "";

        if (types.Any(WikimediaListTypes.Contains) || text.Contains("wikimedia list article", StringComparison.Ordinal))
        {
            return LooksLikePublisherOrProductionList(labelText, text)
                ? WikidataContainerKind.PublisherOrProductionList
                : WikidataContainerKind.WikimediaList;
        }

        if (LooksLikePublisherOrProductionList(labelText, text))
            return WikidataContainerKind.PublisherOrProductionList;

        if (types.Any(MangaSeriesTypes.Contains) ||
            text.Contains("manga series", StringComparison.Ordinal))
        {
            return WikidataContainerKind.MangaSeries;
        }

        if (types.Any(ComicSeriesTypes.Contains) ||
            text.Contains("graphic novel", StringComparison.Ordinal) ||
            text.Contains("comic book series", StringComparison.Ordinal) ||
            text.Contains("comics series", StringComparison.Ordinal))
        {
            return WikidataContainerKind.ComicSeries;
        }

        if (types.Any(TvSeasonTypes.Contains) ||
            text.Contains("television season", StringComparison.Ordinal) ||
            text.Contains("tv season", StringComparison.Ordinal))
        {
            return WikidataContainerKind.TvSeason;
        }

        if (types.Any(TvShowTypes.Contains) ||
            text.Contains("television series", StringComparison.Ordinal) ||
            text.Contains("tv series", StringComparison.Ordinal))
        {
            return WikidataContainerKind.TvShow;
        }

        if (types.Any(AlbumTypes.Contains) ||
            text.Contains("studio album", StringComparison.Ordinal) ||
            text.Contains("live album", StringComparison.Ordinal) ||
            text.Contains("album release", StringComparison.Ordinal))
        {
            return WikidataContainerKind.AlbumRelease;
        }

        if (types.Any(OrderedSeriesTypes.Contains) || LooksLikeOrderedSeriesText(labelText, text))
        {
            return WikidataContainerKind.OrderedSeries;
        }

        if (types.Any(FranchiseTypes.Contains) || text.Contains("franchise", StringComparison.Ordinal))
            return WikidataContainerKind.Franchise;

        if (text.Contains("fictional universe", StringComparison.Ordinal) ||
            text.Contains("shared universe", StringComparison.Ordinal) ||
            text.EndsWith(" universe", StringComparison.Ordinal) ||
            text.Contains(" universe ", StringComparison.Ordinal))
        {
            return WikidataContainerKind.Universe;
        }

        if (labelText.EndsWith(" series", StringComparison.Ordinal) ||
            text.Contains(" series ", StringComparison.Ordinal))
        {
            return WikidataContainerKind.OrderedSeries;
        }

        return WikidataContainerKind.Unknown;
    }

    public static bool IsImmediateSeriesKind(WikidataContainerKind kind)
        => kind is WikidataContainerKind.OrderedSeries
            or WikidataContainerKind.AlbumRelease
            or WikidataContainerKind.TvShow
            or WikidataContainerKind.TvSeason
            or WikidataContainerKind.ComicSeries
            or WikidataContainerKind.MangaSeries;

    public static bool IsRejectedShelfKind(WikidataContainerKind kind)
        => kind is WikidataContainerKind.WikimediaList
            or WikidataContainerKind.PublisherOrProductionList
            or WikidataContainerKind.Franchise
            or WikidataContainerKind.Universe;

    private static bool LooksLikePublisherOrProductionList(string label, string text)
    {
        if (!label.StartsWith("list of ", StringComparison.Ordinal))
            return false;

        return text.Contains("production", StringComparison.Ordinal) ||
               text.Contains("productions", StringComparison.Ordinal) ||
               text.Contains("publisher", StringComparison.Ordinal) ||
               text.Contains("publishing", StringComparison.Ordinal) ||
               text.Contains("studio", StringComparison.Ordinal) ||
               text.Contains("filmography", StringComparison.Ordinal);
    }

    private static bool LooksLikeOrderedSeriesText(string label, string text)
        => text.Contains("film series", StringComparison.Ordinal) ||
           text.Contains("animated film series", StringComparison.Ordinal) ||
           text.Contains("animated film franchise", StringComparison.Ordinal) ||
           text.Contains("book series", StringComparison.Ordinal) ||
           text.Contains("novel series", StringComparison.Ordinal) ||
           text.Contains("video game series", StringComparison.Ordinal) ||
           text.Contains("trilogy", StringComparison.Ordinal) ||
           text.Contains("tetralogy", StringComparison.Ordinal) ||
           label.EndsWith(" film series", StringComparison.Ordinal);

    private static List<string> GetEntityIds(WikidataEntityInfo entity, string propertyId)
    {
        if (!entity.Claims.TryGetValue(propertyId, out var claims))
            return [];

        return claims
            .Where(claim => !string.Equals(claim.Rank, "deprecated", StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value?.EntityId)
            .Where(qid => !string.IsNullOrWhiteSpace(qid))
            .Select(qid => qid!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
