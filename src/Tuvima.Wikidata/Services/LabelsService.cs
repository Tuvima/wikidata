using Tuvima.Wikidata.Internal;

namespace Tuvima.Wikidata.Services;

/// <summary>
/// Single-entity and batch label lookup with language fallback.
/// Obtained via <see cref="WikidataReconciler.Labels"/>.
/// </summary>
public sealed class LabelsService
{
    private readonly ReconcilerContext _ctx;

    internal LabelsService(ReconcilerContext ctx) => _ctx = ctx;

    /// <summary>
    /// Gets the display label for a single Wikidata entity.
    /// </summary>
    /// <param name="qid">The Wikidata QID (e.g., "Q42").</param>
    /// <param name="language">Preferred language. Defaults to the configured language.</param>
    /// <param name="withFallbackLanguage">
    /// When true (default), applies the language fallback chain (e.g., "de-ch" → "de" → "mul" → "en")
    /// if no label exists in the requested language. When false, returns null if the exact language has no label.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The label, or null if the entity does not exist or has no label in the requested language (and fallback didn't resolve one).</returns>
    public async Task<string?> GetAsync(
        string qid,
        string? language = null,
        bool withFallbackLanguage = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qid);

        var lang = language ?? _ctx.Options.Language;
        var result = await GetBatchAsync([qid], lang, withFallbackLanguage, cancellationToken)
            .ConfigureAwait(false);

        return result.TryGetValue(qid, out var label) ? label : null;
    }

    /// <summary>
    /// Gets display labels for multiple Wikidata entities.
    /// </summary>
    /// <param name="qids">The Wikidata QIDs to look up.</param>
    /// <param name="language">Preferred language. Defaults to the configured language.</param>
    /// <param name="withFallbackLanguage">
    /// When true (default), applies the language fallback chain per entity.
    /// When false, returns null for entries that have no label in the exact requested language.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A dictionary containing every input QID. The value is the resolved label, or null if
    /// the entity exists but has no label in the requested language (respecting the fallback flag).
    /// Entities that don't exist at all are absent from the dictionary.
    /// </returns>
    public async Task<IReadOnlyDictionary<string, string?>> GetBatchAsync(
        IReadOnlyList<string> qids,
        string? language = null,
        bool withFallbackLanguage = true,
        CancellationToken cancellationToken = default)
    {
        if (qids.Count == 0)
            return new Dictionary<string, string?>();

        var lang = language ?? _ctx.Options.Language;
        var entities = await _ctx.EntityFetcher.FetchLabelsOnlyAsync(qids.ToList(), lang, cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (id, entity) in entities)
        {
            string? label = null;

            if (withFallbackLanguage)
            {
                LanguageFallback.TryGetValue(entity.Labels, lang, out label);
            }
            else if (entity.Labels?.TryGetValue(lang, out var langValue) == true)
            {
                label = langValue.Value;
            }

            result[id] = string.IsNullOrEmpty(label) ? null : label;
        }

        return result;
    }
}
