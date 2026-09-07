using System.Collections.Concurrent;
using System.Text.Json;
using Tuvima.Wikidata.Internal.Json;

namespace Tuvima.Wikidata.Internal;

/// <summary>
/// Resolves P279 hierarchies using breadth-first traversal and cached direct parents.
/// Each lookup applies its own depth limit; target-specific partial traversals are never cached.
/// </summary>
internal sealed class SubclassResolver
{
    private readonly WikidataEntityFetcher _fetcher;
    private readonly int _maxDepth;
    private readonly ConcurrentDictionary<string, string[]> _parentCache = new(StringComparer.OrdinalIgnoreCase);

    public SubclassResolver(WikidataEntityFetcher fetcher, int maxDepth)
    {
        _fetcher = fetcher;
        _maxDepth = maxDepth;
    }

    public async Task<bool> IsSubclassOfAsync(
        IReadOnlyList<string> entityTypeQids,
        string targetTypeQid,
        string language,
        CancellationToken cancellationToken,
        int? overrideDepth = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var visited = new HashSet<string>(entityTypeQids, StringComparer.OrdinalIgnoreCase);
        if (visited.Contains(targetTypeQid))
            return true;

        var frontier = new HashSet<string>(visited, StringComparer.OrdinalIgnoreCase);
        var maxDepth = Math.Max(0, overrideDepth ?? _maxDepth);
        for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var missing = frontier.Where(qid => !_parentCache.ContainsKey(qid)).ToList();
            if (missing.Count > 0)
            {
                var entities = await _fetcher.FetchEntitiesAsync(missing, language, cancellationToken).ConfigureAwait(false);
                foreach (var qid in missing)
                {
                    // A missing entity is not proof that it has no parents; don't cache absence.
                    if (entities.TryGetValue(qid, out var entity))
                        _parentCache.TryAdd(qid, GetP279Values(entity));
                }
            }

            var next = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var qid in frontier)
            {
                if (!_parentCache.TryGetValue(qid, out var parents))
                    continue;
                foreach (var parent in parents)
                {
                    if (string.Equals(parent, targetTypeQid, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (visited.Add(parent))
                        next.Add(parent);
                }
            }
            frontier = next;
        }
        return false;
    }

    private static string[] GetP279Values(WikidataEntity entity)
    {
        if (entity.Claims?.TryGetValue("P279", out var claims) != true)
            return [];

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in claims!)
        {
            if (claim.MainSnak?.SnakType == "value" && claim.MainSnak.DataValue?.Value is JsonElement element &&
                element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var idProp) &&
                idProp.ValueKind == JsonValueKind.String && idProp.GetString() is { Length: > 0 } id)
                values.Add(id);
        }
        return values.ToArray();
    }
}
