using Tuvima.Wikidata.Internal;
using Tuvima.Wikidata.Internal.Json;

namespace Tuvima.Wikidata.Tests;

public class LanguageFallbackTests
{
    [Fact]
    public void ExactMatch_ReturnsValue()
    {
        var dict = new Dictionary<string, LanguageValue>
        {
            ["en"] = new() { Language = "en", Value = "English" },
            ["de"] = new() { Language = "de", Value = "German" }
        };

        Assert.True(LanguageFallback.TryGetValue(dict, "de", out var value));
        Assert.Equal("German", value);
    }

    [Fact]
    public void SubtagFallback_ReturnsParent()
    {
        var dict = new Dictionary<string, LanguageValue>
        {
            ["de"] = new() { Language = "de", Value = "German" }
        };

        Assert.True(LanguageFallback.TryGetValue(dict, "de-ch", out var value));
        Assert.Equal("German", value);
    }

    [Fact]
    public void MulFallback_ReturnsMultilingual()
    {
        var dict = new Dictionary<string, LanguageValue>
        {
            ["mul"] = new() { Language = "mul", Value = "Multilingual" }
        };

        Assert.True(LanguageFallback.TryGetValue(dict, "sw", out var value));
        Assert.Equal("Multilingual", value);
    }

    [Fact]
    public void EnglishFallback_ReturnsEnglish()
    {
        var dict = new Dictionary<string, LanguageValue>
        {
            ["en"] = new() { Language = "en", Value = "English" }
        };

        Assert.True(LanguageFallback.TryGetValue(dict, "sw", out var value));
        Assert.Equal("English", value);
    }

    [Fact]
    public void NoMatch_ReturnsFalse()
    {
        var dict = new Dictionary<string, LanguageValue>
        {
            ["fr"] = new() { Language = "fr", Value = "French" }
        };

        Assert.False(LanguageFallback.TryGetValue(dict, "de", out _));
    }

    [Fact]
    public void NullDict_ReturnsFalse()
    {
        Assert.False(LanguageFallback.TryGetValue(null, "en", out _));
    }

    [Fact]
    public void BuildLanguageParam_IncludesFallbacks()
    {
        var param = LanguageFallback.BuildLanguageParam("de-ch");
        Assert.Contains("de-ch", param);
        Assert.Contains("de", param);
        Assert.Contains("mul", param);
        Assert.Contains("en", param);
    }

    [Fact]
    public void BuildLanguageParam_English_NoDuplicates()
    {
        var param = LanguageFallback.BuildLanguageParam("en");
        // "en" should only appear once, plus "mul"
        var parts = param.Split('|');
        Assert.Equal(parts.Distinct().Count(), parts.Length);
    }

    [Fact]
    public void FallbackChain_PriorityOrder()
    {
        var chain = LanguageFallback.GetFallbackChain("de-ch");
        Assert.Equal(["de-ch", "de", "mul", "en"], chain);
    }
}
