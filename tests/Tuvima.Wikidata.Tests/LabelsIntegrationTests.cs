namespace Tuvima.Wikidata.Tests;

/// <summary>
/// Integration tests for the v2.0.0 LabelsService against the live Wikidata API.
/// </summary>
[Trait("Category", "Integration")]
public class LabelsIntegrationTests : IDisposable
{
    private readonly WikidataReconciler _reconciler;

    public LabelsIntegrationTests()
    {
        _reconciler = new WikidataReconciler(new WikidataReconcilerOptions
        {
            UserAgent = "Tuvima.Wikidata.Tests/2.2 (https://github.com/Tuvima/wikidata)"
        });
    }

    [Fact]
    public async Task GetAsync_DouglasAdams_ReturnsEnglishLabel()
    {
        var label = await _reconciler.Labels.GetAsync("Q42");

        Assert.NotNull(label);
        Assert.Equal("Douglas Adams", label);
    }

    [Fact]
    public async Task GetAsync_DouglasAdams_InGerman_ReturnsLocalizedLabel()
    {
        var label = await _reconciler.Labels.GetAsync("Q42", language: "de");

        Assert.NotNull(label);
        // Wikidata's German label for Douglas Adams is "Douglas Adams" as well, but
        // the call exercises the language param path. Just assert we got *something*.
        Assert.False(string.IsNullOrEmpty(label));
    }

    [Fact]
    public async Task GetBatchAsync_AllInputsPresentInResult()
    {
        var qids = new[] { "Q42", "Q5", "Q183" };
        var result = await _reconciler.Labels.GetBatchAsync(qids);

        Assert.Equal(3, result.Count);
        Assert.Contains("Q42", result.Keys);
        Assert.Contains("Q5", result.Keys);
        Assert.Contains("Q183", result.Keys);
        Assert.Equal("Douglas Adams", result["Q42"]);
        Assert.Equal("human", result["Q5"]);
    }

    [Fact]
    public async Task GetBatchAsync_OnlyNonexistentQid_ResultOmitsIt()
    {
        // Wikidata's wbgetentities API fails the entire batch when any QID is malformed,
        // so we probe the nonexistent case in isolation. The entity is absent from the dict
        // (not present-with-null).
        var result = await _reconciler.Labels.GetBatchAsync(["Q999999999999"]);

        Assert.False(result.ContainsKey("Q999999999999"));
    }

    [Fact]
    public async Task GetAsync_NonexistentQid_ReturnsNull()
    {
        var label = await _reconciler.Labels.GetAsync("Q999999999999");

        Assert.Null(label);
    }

    [Fact]
    public async Task GetBatchAsync_EmptyInput_ReturnsEmptyDict()
    {
        var result = await _reconciler.Labels.GetBatchAsync([]);
        Assert.Empty(result);
    }

    public void Dispose() => _reconciler.Dispose();
}
