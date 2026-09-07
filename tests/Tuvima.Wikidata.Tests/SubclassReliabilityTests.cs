using Tuvima.Wikidata.Internal;

namespace Tuvima.Wikidata.Tests;

public class SubclassReliabilityTests
{
    [Fact]
    public async Task LookupOrderAndDepth_DoNotChangeReachability()
    {
        using var http = new HttpClient(HierarchyHandler());
        using var pipeline = new ResilientHttpClient(http, Phase1HttpReliabilityTests.Options(), new());
        var fetcher = new WikidataEntityFetcher(pipeline, Phase1HttpReliabilityTests.Options(), new());
        var resolver = new SubclassResolver(fetcher, 3);
        Assert.True(await resolver.IsSubclassOfAsync(["Q1"], "Q2", "en", default));
        Assert.True(await resolver.IsSubclassOfAsync(["Q1"], "Q3", "en", default));
        Assert.False(await resolver.IsSubclassOfAsync(["Q1"], "Q3", "en", default, overrideDepth: 1));
        Assert.False(await resolver.IsSubclassOfAsync(["Q1"], "Q2", "en", default, overrideDepth: 0));
    }

    [Fact]
    public async Task ShallowMiss_DoesNotPreventDeeperLookup_AndCyclesTerminate()
    {
        using var http = new HttpClient(HierarchyHandler());
        using var pipeline = new ResilientHttpClient(http, Phase1HttpReliabilityTests.Options(), new());
        var resolver = new SubclassResolver(new WikidataEntityFetcher(pipeline, Phase1HttpReliabilityTests.Options(), new()), 1);
        Assert.False(await resolver.IsSubclassOfAsync(["Q1"], "Q3", "en", default));
        Assert.True(await resolver.IsSubclassOfAsync(["Q1"], "Q3", "en", default, overrideDepth: 2));
        Assert.False(await resolver.IsSubclassOfAsync(["Q1"], "Q99", "en", default, overrideDepth: 100));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            resolver.IsSubclassOfAsync(["Q1"], "Q2", "en", cancellation.Token));
    }

    [Theory]
    [InlineData(3, 0, false, false)]
    [InlineData(1, 2, true, false)]
    [InlineData(3, 1, false, false)]
    [InlineData(0, 2, true, false)]
    [InlineData(1, 2, true, true)]
    [InlineData(3, 0, false, true)]
    public async Task PublicRequestDepth_OverridesGlobalDepth(int globalDepth, int requestDepth, bool expected, bool textQuery)
    {
        using var http = new HttpClient(HierarchyHandler());
        using var reconciler = new WikidataReconciler(http, new()
        {
            TypeHierarchyDepth = globalDepth,
            WikidataRateLimit = ProviderRateLimitOptions.Unthrottled
        });
        var results = await reconciler.Reconcile.ReconcileAsync(new ReconciliationRequest()
        {
            Query = textQuery ? "Example" : "Q10", Types = ["Q3"], TypeHierarchyDepth = requestDepth
        });
        Assert.Equal(expected, results.Count == 1);
        var excluded = await reconciler.Reconcile.ReconcileAsync(new ReconciliationRequest
        {
            Query = textQuery ? "Example" : "Q10", ExcludeTypes = ["Q3"], TypeHierarchyDepth = requestDepth
        });
        Assert.Equal(!expected, excluded.Count == 1);
    }

    private static TestHttpMessageHandler HierarchyHandler() => new((request, _) =>
    {
        if (request.RequestUri!.Query.Contains("action=wbsearchentities", StringComparison.Ordinal))
            return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.SearchResponse(("Q10", "Example"))));
        if (request.RequestUri.Query.Contains("action=query", StringComparison.Ordinal))
            return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.QueryResponse("Q10")));
        var ids = Uri.UnescapeDataString(request.RequestUri!.Query).Split('&')
            .First(part => part.TrimStart('?').StartsWith("ids=", StringComparison.Ordinal)).Split('=')[1].Split('|');
        var entities = ids.Select(id => TestPayloads.Entity(id, id,
            id == "Q10"
                ? TestPayloads.Claims(("P31", "wikibase-item", TestPayloads.ItemDataValue("Q1"), "normal"))
                : id is "Q1" or "Q2"
                    ? TestPayloads.Claims(("P279", "wikibase-item", TestPayloads.ItemDataValue(id == "Q1" ? "Q2" : "Q3"), "normal"))
                    : id == "Q3" ? TestPayloads.Claims(("P279", "wikibase-item", TestPayloads.ItemDataValue("Q1"), "normal")) : null)).ToArray();
        return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.EntityResponse(entities)));
    });
}

