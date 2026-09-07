namespace Tuvima.Wikidata.Tests;

public class BridgeContractTests
{
    [Theory]
    [InlineData(BridgeRollupTarget.ResolvedEntity, "Q1", "Q1", false)]
    [InlineData(BridgeRollupTarget.PreferCanonicalWork, "Q10", "Q10", true)]
    [InlineData(BridgeRollupTarget.PreferEdition, "Q1", "Q10", true)]
    [InlineData(BridgeRollupTarget.ReturnWorkAndEdition, "Q1", "Q10", true)]
    public async Task EditionRollup_PreservesSelectedCandidateAndPath(
        BridgeRollupTarget target, string resolved, string canonical, bool rollsUp)
    {
        using var reconciler = CreateReconciler("P629", "Q10");
        var result = await reconciler.Bridge.ResolveAsync(Request(target));
        Assert.Equal("Q1", result.SelectedCandidate?.Qid);
        Assert.Equal(resolved, result.Rollup?.ResolvedEntityQid);
        Assert.Equal(canonical, result.Rollup?.CanonicalWorkQid);
        Assert.Equal(rollsUp, result.Rollup?.IsRollup);
        if (rollsUp)
        {
            var step = Assert.Single(result.Rollup!.RelationshipPath);
            Assert.Equal("Q1", step.SubjectQid);
            Assert.Equal("P629", step.PropertyId);
            Assert.Equal("Q10", step.ObjectQid);
            Assert.Equal(Direction.Outgoing, step.Direction);
        }
        else Assert.Empty(result.Rollup!.RelationshipPath);
    }

    [Fact]
    public async Task PreferEdition_ChoosesLowestNumericQid()
    {
        using var reconciler = CreateReconciler("P747", "Q20", "Q3");
        var result = await reconciler.Bridge.ResolveAsync(Request(BridgeRollupTarget.PreferEdition));
        Assert.Equal("Q3", result.Rollup?.ResolvedEntityQid);
        Assert.Equal("Q1", result.Rollup?.CanonicalWorkQid);
        Assert.Equal("P747", Assert.Single(result.Rollup!.RelationshipPath).PropertyId);
    }

    [Fact]
    public async Task EqualCandidates_PreserveNumericTieBreakAndAmbiguityWarning()
    {
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.Query.Contains("action=query", StringComparison.Ordinal))
                return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.QueryResponse("Q20", "Q3")));
            return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.EntityResponse(
                Movie("Q20"), Movie("Q3"))));
        });
        using var reconciler = TestPayloads.CreateReconciler(handler);
        var result = await reconciler.Bridge.ResolveAsync(Request(BridgeRollupTarget.ResolvedEntity));
        Assert.Equal(new[] { "Q3", "Q20" }, result.Candidates.Select(c => c.Qid));
        Assert.Equal(0.95, result.Candidates[0].Confidence);
        Assert.Equal(new[] { "bridge.exact", "type.match", "title.exact" }, result.Candidates[0].ReasonCodes);
        Assert.Contains("candidate.ambiguous", result.Diagnostics.Warnings);
    }

    [Fact]
    public async Task Relationships_PreservePropertyProvenanceAndDeduplicateWithinProperty()
    {
        using var reconciler = CreateReconciler("P361", "Q10", "Q10");
        var result = await reconciler.Bridge.ResolveAsync(Request(BridgeRollupTarget.ResolvedEntity));
        var edge = Assert.Single(result.Relationships);
        Assert.Equal("Q1", edge.SubjectQid);
        Assert.Equal("P361", edge.PropertyId);
        Assert.Equal("Q10", edge.ObjectQid);
        Assert.Equal("Related", edge.ObjectLabel);
        Assert.Equal("parent-work", edge.RelationshipKind);
        Assert.Equal(1.0, edge.Confidence);
        Assert.Equal("P361", Assert.Single(result.Series).SourcePropertyId);
    }

    private static BridgeResolutionRequest Request(BridgeRollupTarget target) => new()
    {
        CorrelationKey = "item", MediaKind = BridgeMediaKind.Movie, Title = "Example",
        BridgeIds = new Dictionary<string, string> { ["imdb_id"] = "tt0000001" }, RollupTarget = target
    };

    private static Dictionary<string, object?> Movie(string qid) => TestPayloads.Entity(qid, "Example", claims: TestPayloads.Claims(
        ("P31", "wikibase-item", TestPayloads.ItemDataValue("Q11424"), "normal"),
        ("P345", "external-id", TestPayloads.StringDataValue("tt0000001"), "normal")));

    private static WikidataReconciler CreateReconciler(string property, params string[] targets)
    {
        var entity = Movie("Q1");
        var claims = (Dictionary<string, object>)entity["claims"]!;
        var relationships = TestPayloads.Claims(targets.Select(target =>
            (property, "wikibase-item", TestPayloads.ItemDataValue(target), "normal")).ToArray());
        claims[property] = relationships[property];
        return TestPayloads.CreateReconciler(new TestHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.Query.Contains("action=query", StringComparison.Ordinal))
                return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.QueryResponse("Q1")));
            return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.EntityResponse(
                new[] { entity }.Concat(targets.Distinct().Select(id => TestPayloads.Entity(id, "Related"))).ToArray())));
        }));
    }
}
