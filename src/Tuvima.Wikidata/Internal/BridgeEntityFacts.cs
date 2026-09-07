namespace Tuvima.Wikidata.Internal;

/// <summary>Shared claim readers and property sets used by bridge collaborators.</summary>
internal static class BridgeEntityFacts
{
    public static readonly string[] CreatorPropertyIds = ["P50", "P57", "P58", "P86", "P162", "P170", "P175", "P676"];

    public static readonly string[] SeriesPropertyIds = ["P179", "P361", "P8345"];

    public static List<string> GetEntityIds(WikidataEntityInfo entity, string propertyId)
    {
        if (!entity.Claims.TryGetValue(propertyId, out var claims))
            return [];

        return claims
            .Select(c => c.Value?.EntityId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string? GetFirstEntityId(WikidataEntityInfo entity, string propertyId)
        => GetEntityIds(entity, propertyId).FirstOrDefault();

    public static string? GetFirstRawValue(WikidataEntityInfo entity, string propertyId)
    {
        return entity.Claims.TryGetValue(propertyId, out var claims)
            ? claims.Select(c => c.Value?.RawValue).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
            : null;
    }

    public static string? TryGetQualifierValue(WikidataClaim claim, string propertyId)
    {
        return claim.Qualifiers.TryGetValue(propertyId, out var values)
            ? values.Select(v => v.RawValue).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
            : null;
    }

    public static long QidNumber(string qid)
    {
        return qid.Length > 1 && long.TryParse(qid.AsSpan(1), out var n)
            ? n
            : long.MaxValue;
    }
}
