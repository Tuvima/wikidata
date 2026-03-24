using Tuvima.WikidataReconciliation.Internal.Json;

namespace Tuvima.WikidataReconciliation.Internal;

/// <summary>
/// Language fallback chain: exact → subtag parent ("de-ch" → "de") → "mul" → "en".
/// </summary>
internal static class LanguageFallback
{
    /// <summary>
    /// Returns the ordered fallback chain for a language code.
    /// </summary>
    public static List<string> GetFallbackChain(string language)
    {
        var chain = new List<string> { language };

        var dash = language.IndexOf('-');
        if (dash > 0)
            chain.Add(language[..dash]);

        if (!chain.Contains("mul"))
            chain.Add("mul");
        if (!chain.Contains("en"))
            chain.Add("en");

        return chain;
    }

    /// <summary>
    /// Builds the language parameter for Wikidata API requests.
    /// Includes fallback languages so the response contains data for the chain.
    /// </summary>
    public static string BuildLanguageParam(string language)
    {
        var chain = GetFallbackChain(language);
        return string.Join('|', chain);
    }

    /// <summary>
    /// Tries to get a label/description value using the fallback chain.
    /// </summary>
    public static bool TryGetValue(
        Dictionary<string, LanguageValue>? dict, string language, out string value)
    {
        value = "";
        if (dict is null)
            return false;

        foreach (var lang in GetFallbackChain(language))
        {
            if (dict.TryGetValue(lang, out var lv) && !string.IsNullOrEmpty(lv.Value))
            {
                value = lv.Value;
                return true;
            }
        }

        return false;
    }
}
