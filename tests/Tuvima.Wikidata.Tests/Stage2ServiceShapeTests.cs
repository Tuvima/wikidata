namespace Tuvima.Wikidata.Tests;

/// <summary>
/// Contract tests for the v2.2.0 Stage2Service — verifies discriminated-type dispatch,
/// static factory behavior, and the strict "no unfiltered text" rule without hitting the network.
/// </summary>
public class Stage2ServiceShapeTests
{
    [Fact]
    public void Facade_ExposesStage2Service()
    {
        using var reconciler = new WikidataReconciler();
        Assert.NotNull(reconciler.Stage2);
    }

    [Fact]
    public void Stage2Request_BridgeFactory_BuildsConcreteType()
    {
        var req = Stage2Request.Bridge(
            correlationKey: "row-1",
            bridgeIds: new Dictionary<string, string> { ["isbn13"] = "9780441172719" },
            wikidataProperties: new Dictionary<string, string> { ["isbn13"] = "P212" });

        Assert.IsType<BridgeStage2Request>(req);
        Assert.Equal("row-1", req.CorrelationKey);
        Assert.Single(req.BridgeIds);
        Assert.Null(req.EditionPivot);
    }

    [Fact]
    public void Stage2Request_MusicFactory_BuildsConcreteType()
    {
        var req = Stage2Request.Music("row-2", "Random Access Memories", "Daft Punk");

        Assert.IsType<MusicStage2Request>(req);
        Assert.Equal("Random Access Memories", req.AlbumTitle);
        Assert.Equal("Daft Punk", req.Artist);
    }

    [Fact]
    public void Stage2Request_TextFactory_BuildsConcreteType()
    {
        var req = Stage2Request.Text(
            correlationKey: "row-3",
            title: "1984",
            cirrusSearchTypes: ["Q7725634"],
            author: "George Orwell");

        Assert.IsType<TextStage2Request>(req);
        Assert.Equal("1984", req.Title);
        Assert.Single(req.CirrusSearchTypes);
        Assert.False(req.AllowUnfilteredText);
        Assert.Equal(0.70, req.AcceptThreshold);
    }

    [Fact]
    public async Task Stage2_TextRequest_WithEmptyTypesAndNoOptIn_Throws()
    {
        using var reconciler = new WikidataReconciler();

        var req = new TextStage2Request
        {
            CorrelationKey = "bad",
            Title = "Some Title",
            CirrusSearchTypes = []
        };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await reconciler.Stage2.ResolveAsync(req));
    }

    [Fact]
    public async Task Stage2_EmptyBatch_ReturnsEmptyDictionary()
    {
        using var reconciler = new WikidataReconciler();
        var result = await reconciler.Stage2.ResolveBatchAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public void Stage2Result_NotFound_SingletonShape()
    {
        var notFound = Stage2Result.NotFound;

        Assert.False(notFound.Found);
        Assert.Null(notFound.Qid);
        Assert.Null(notFound.WorkQid);
        Assert.Null(notFound.EditionQid);
        Assert.False(notFound.IsEdition);
        Assert.Equal(Stage2MatchedStrategy.NotResolved, notFound.MatchedBy);
        Assert.Empty(notFound.CollectedBridgeIds);
    }

    [Fact]
    public void EditionPivotRule_EmptyDefaults()
    {
        var rule = new EditionPivotRule();

        Assert.Empty(rule.WorkClasses);
        Assert.Empty(rule.EditionClasses);
        Assert.False(rule.PreferEdition);
        Assert.Null(rule.RankingHints);
    }

    [Fact]
    public void RankingHint_RequiresPropertyAndValues()
    {
        var hint = new RankingHint
        {
            PropertyId = "P175",
            Values = ["Stephen Fry", "Jim Dale"]
        };

        Assert.Equal("P175", hint.PropertyId);
        Assert.Equal(2, hint.Values.Count);
        Assert.Equal(1.0, hint.Weight);
    }

    [Fact]
    public void BridgeStage2Request_PreferredOrderIsOptional()
    {
        var req = Stage2Request.Bridge(
            "row",
            new Dictionary<string, string> { ["isbn"] = "x", ["ol"] = "y" },
            new Dictionary<string, string> { ["isbn"] = "P212", ["ol"] = "P648" });

        Assert.Null(req.PreferredOrder);
    }

    [Fact]
    public async Task Stage2_UnsupportedImplementationOfIStage2Request_ThrowsNotSupported()
    {
        using var reconciler = new WikidataReconciler();
        var weirdImpl = new WeirdStage2Request();

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await reconciler.Stage2.ResolveBatchAsync([weirdImpl]));
    }

    private sealed class WeirdStage2Request : IStage2Request
    {
        public string CorrelationKey => "weird";
    }
}
