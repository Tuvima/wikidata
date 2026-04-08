using Tuvima.Wikidata.Internal;

namespace Tuvima.Wikidata.Services;

/// <summary>
/// Work-to-edition pivoting operations (P747 / P629).
/// Obtained via <see cref="WikidataReconciler.Editions"/>.
/// </summary>
public sealed class EditionService
{
    private readonly ReconcilerContext _ctx;

    internal EditionService(ReconcilerContext ctx) => _ctx = ctx;

    /// <summary>
    /// Fetches editions and translations (P747) of a work entity.
    /// Optionally filters by P31 type.
    /// </summary>
    public async Task<IReadOnlyList<EditionInfo>> GetEditionsAsync(
        string workQid, IReadOnlyList<string>? filterTypes = null,
        string? language = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workQid);

        var lang = language ?? _ctx.Options.Language;

        var workEntities = await _ctx.EntityFetcher.FetchEntitiesAsync([workQid], lang, cancellationToken)
            .ConfigureAwait(false);

        if (!workEntities.TryGetValue(workQid, out var workEntity))
            return [];

        var editionIds = WikidataEntityFetcher.GetClaimValues(workEntity, "P747")
            .Select(dv => EntityMapper.MapDataValue(dv, "wikibase-item"))
            .Where(v => v.Kind == WikidataValueKind.EntityId && !string.IsNullOrEmpty(v.EntityId))
            .Select(v => v.EntityId!)
            .ToList();

        if (editionIds.Count == 0)
            return [];

        var editionEntities = await _ctx.EntityFetcher.FetchEntitiesAsync(editionIds, lang, cancellationToken)
            .ConfigureAwait(false);

        var filterSet = filterTypes is { Count: > 0 }
            ? new HashSet<string>(filterTypes, StringComparer.OrdinalIgnoreCase)
            : null;

        var results = new List<EditionInfo>();
        foreach (var (id, entity) in editionEntities)
        {
            var types = WikidataEntityFetcher.GetTypeIds(entity, _ctx.Options.TypePropertyId);

            if (filterSet is not null && !types.Any(t => filterSet.Contains(t)))
                continue;

            LanguageFallback.TryGetValue(entity.Labels, lang, out var label);
            LanguageFallback.TryGetValue(entity.Descriptions, lang, out var description);

            results.Add(new EditionInfo
            {
                EntityId = id,
                Label = string.IsNullOrEmpty(label) ? null : label,
                Description = string.IsNullOrEmpty(description) ? null : description,
                Types = types,
                Claims = EntityMapper.MapClaims(entity.Claims)
            });
        }

        return results;
    }

    /// <summary>
    /// Given an edition QID, finds the parent work via P629.
    /// </summary>
    public async Task<WikidataEntityInfo?> GetWorkForEditionAsync(
        string editionQid, string? language = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(editionQid);

        var lang = language ?? _ctx.Options.Language;
        var editionEntities = await _ctx.EntityFetcher.FetchEntitiesAsync([editionQid], lang, cancellationToken)
            .ConfigureAwait(false);

        if (!editionEntities.TryGetValue(editionQid, out var editionEntity))
            return null;

        var workIds = WikidataEntityFetcher.GetClaimValues(editionEntity, "P629")
            .Select(dv => EntityMapper.MapDataValue(dv, "wikibase-item"))
            .Where(v => v.Kind == WikidataValueKind.EntityId && !string.IsNullOrEmpty(v.EntityId))
            .Select(v => v.EntityId!)
            .ToList();

        if (workIds.Count == 0)
            return null;

        var workEntities = await _ctx.EntityFetcher.FetchEntitiesAsync([workIds[0]], lang, cancellationToken)
            .ConfigureAwait(false);

        return workEntities.TryGetValue(workIds[0], out var workEntity)
            ? EntityMapper.MapEntity(workEntity, lang)
            : null;
    }
}
