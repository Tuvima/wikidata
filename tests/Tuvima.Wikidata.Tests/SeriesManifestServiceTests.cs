using System.Text.RegularExpressions;

namespace Tuvima.Wikidata.Tests;

public class SeriesManifestServiceTests
{
    [Fact]
    public async Task GetManifestAsync_IncomingP179_OrdersBySeriesOrdinalQualifier()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["Q1"] = Entity("Q1", "Book One", Claims(ItemClaim("P179", "QSeries", "1"))),
                ["Q2"] = Entity("Q2", "Book Two", Claims(ItemClaim("P179", "QSeries", "2")))
            },
            p179: ["Q2", "Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Equal(["Q1", "Q2"], manifest.Items.Select(i => i.Qid));
        Assert.Equal(1m, manifest.Items[0].ParsedSeriesOrdinal);
        Assert.Equal("1", manifest.Items[0].RawSeriesOrdinal);
        Assert.Equal(SeriesManifestOrderSource.SeriesOrdinal, manifest.Items[0].OrderSource);
        Assert.Contains("P179", manifest.Items[0].SourceProperties);
        Assert.All(manifest.Items, item => Assert.Equal(SeriesManifestItemScope.MainSequence, item.MembershipScope));
    }

    [Fact]
    public async Task GetManifestAsync_IncomingP361_PreservesPartOfSource()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["Q1"] = Entity("Q1", "Part", Claims(ItemClaim("P361", "QSeries", "1")))
            },
            p361: ["Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        var item = Assert.Single(manifest.Items);
        Assert.Equal("Q1", item.Qid);
        Assert.Equal(["P361"], item.SourceProperties);
        Assert.Equal(SeriesManifestItemScope.Supplementary, item.MembershipScope);
        Assert.Contains(item.Relationships, r => r.PropertyId == "P361" && r.TargetQid == "QSeries" && r.Direction == "Outgoing");
        Assert.Contains(manifest.ExpectedCounts, fact =>
            fact.Scope == SeriesManifestItemScope.Supplementary && fact.Count == 1);
    }

    [Fact]
    public async Task GetManifestAsync_UnpositionedDirectMember_DoesNotInflatePositionedMainSequence()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["Q1"] = Entity("Q1", "Book One", Claims(ItemClaim("P179", "QSeries", "1"))),
                ["Q2"] = Entity("Q2", "Book Two", Claims(ItemClaim("P179", "QSeries", "2"))),
                ["QExtra"] = Entity("QExtra", "Unnumbered novella", Claims(ItemClaim("P179", "QSeries")))
            },
            p179: ["QExtra", "Q2", "Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Equal(SeriesManifestItemScope.Unpositioned, Assert.Single(manifest.Items, item => item.Qid == "QExtra").MembershipScope);
        Assert.Contains(manifest.ExpectedCounts, fact => fact.Scope == SeriesManifestItemScope.MainSequence && fact.Count == 2);
        Assert.Contains(manifest.ExpectedCounts, fact => fact.Scope == SeriesManifestItemScope.Unpositioned && fact.Count == 1);
    }

    [Fact]
    public async Task GetManifestAsync_IncomingP8345_DoesNotExpandFranchiseByDefault()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Film Franchise", Claims(ItemClaim("P31", "Q196600"))),
                ["Q1"] = Entity("Q1", "First Film", Claims(ItemClaim("P8345", "QSeries"), ItemClaim("P156", "Q2"))),
                ["Q2"] = Entity("Q2", "Second Film", Claims(ItemClaim("P8345", "QSeries"), ItemClaim("P155", "Q1")))
            },
            p8345: ["Q2", "Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Empty(manifest.Items);
        Assert.Equal(WikidataContainerKind.Franchise, manifest.ContainerKind);
        Assert.Contains(manifest.Warnings, w => w.Code == "UnsupportedContainerKind");
    }

    [Fact]
    public async Task GetManifestAsync_IncomingP8345_CanExpandFranchiseWhenRequested()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Film Franchise", Claims(ItemClaim("P31", "Q196600"))),
                ["Q1"] = Entity("Q1", "First Film", Claims(ItemClaim("P8345", "QSeries"), ItemClaim("P156", "Q2"))),
                ["Q2"] = Entity("Q2", "Second Film", Claims(ItemClaim("P8345", "QSeries"), ItemClaim("P155", "Q1")))
            },
            p8345: ["Q2", "Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync(new SeriesManifestRequest
        {
            SeriesQid = "QSeries",
            IncludeFranchiseMembers = true
        });

        Assert.Equal(["Q1", "Q2"], manifest.Items.Select(i => i.Qid));
        Assert.All(manifest.Items, item => Assert.Contains("P8345", item.SourceProperties));
        Assert.All(manifest.Items, item => Assert.Equal(SeriesManifestItemScope.BroaderContext, item.MembershipScope));
        Assert.Contains(manifest.Items[0].Relationships, r => r.PropertyId == "P8345" && r.TargetQid == "QSeries" && r.Direction == "Outgoing");
        Assert.All(manifest.Items, item => Assert.Equal(SeriesManifestOrderSource.PreviousNextChain, item.OrderSource));
    }

    [Fact]
    public async Task GetManifestAsync_SpiderVerseClassifiesOrderedFilmSeriesAndCountsRows()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["Q99601314"] = Entity("Q99601314", "Spider-Verse", Claims(ItemClaim("P31", "Q24856"))),
                ["QFilm1"] = Entity("QFilm1", "Spider-Man: Into the Spider-Verse", Claims(ItemClaim("P179", "Q99601314", "1"))),
                ["QFilm2"] = Entity("QFilm2", "Spider-Man: Across the Spider-Verse", Claims(ItemClaim("P179", "Q99601314", "2"))),
                ["QFilm3"] = Entity("QFilm3", "Spider-Man: Beyond the Spider-Verse", Claims(ItemClaim("P179", "Q99601314", "3"))),
                ["QFilm4"] = Entity("QFilm4", "Untitled Spider-Verse film", Claims(ItemClaim("P179", "Q99601314", "4")))
            },
            p179: ["QFilm4", "QFilm2", "QFilm1", "QFilm3"]);

        var manifest = await reconciler.Series.GetManifestAsync("Q99601314");

        Assert.Equal(WikidataContainerKind.OrderedSeries, manifest.ContainerKind);
        Assert.Equal(["QFilm1", "QFilm2", "QFilm3", "QFilm4"], manifest.Items.Select(i => i.Qid));
        Assert.Contains(manifest.ExpectedCounts, f => f.Kind == "manifest_items" && f.Count == 4);
    }

    [Fact]
    public async Task GetManifestAsync_SonyPicturesAnimationListIsDiagnosticOnly()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["Q65071834"] = Entity("Q65071834", "list of Sony Pictures Animation productions", Claims(ItemClaim("P31", "Q13406463"))),
                ["QFilm"] = Entity("QFilm", "Open Season", Claims(ItemClaim("P179", "Q65071834", "1")))
            },
            p179: ["QFilm"]);

        var manifest = await reconciler.Series.GetManifestAsync("Q65071834");

        Assert.Empty(manifest.Items);
        Assert.Equal(WikidataContainerKind.PublisherOrProductionList, manifest.ContainerKind);
        Assert.Contains(manifest.Warnings, w => w.Code == "UnsupportedContainerKind");
    }

    [Fact]
    public async Task GetManifestAsync_SparseComicManifestDoesNotSynthesizeTitleSpecificCountFacts()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["Q827099"] = Entity("Q827099", "The Sandman", Claims(ItemClaim("P31", "Q1004")))
            });

        var manifest = await reconciler.Series.GetManifestAsync("Q827099");

        Assert.Equal(WikidataContainerKind.ComicSeries, manifest.ContainerKind);
        Assert.Empty(manifest.ExpectedCounts);
    }

    [Fact]
    public async Task GetManifestAsync_ComicManifestCountsConcreteChildRows()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QComicSeries"] = Entity("QComicSeries", "Comic Run", Claims(ItemClaim("P31", "Q1004"))),
                ["QIssue1"] = Entity("QIssue1", "Issue 1", Claims(ItemClaim("P179", "QComicSeries", "1"))),
                ["QIssue2"] = Entity("QIssue2", "Issue 2", Claims(ItemClaim("P179", "QComicSeries", "2")))
            },
            p179: ["QIssue2", "QIssue1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QComicSeries");

        Assert.Equal(WikidataContainerKind.ComicSeries, manifest.ContainerKind);
        Assert.Contains(manifest.ExpectedCounts, f => f.Kind == "issues" && f.Count == 2);
    }

    [Fact]
    public async Task GetManifestAsync_SparseMangaManifestDoesNotSynthesizeTitleSpecificCountFacts()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["Q91486"] = Entity("Q91486", "Akira", Claims(ItemClaim("P31", "Q21198342")))
            });

        var manifest = await reconciler.Series.GetManifestAsync("Q91486");

        Assert.Equal(WikidataContainerKind.MangaSeries, manifest.ContainerKind);
        Assert.Empty(manifest.ExpectedCounts);
    }

    [Fact]
    public async Task GetManifestAsync_OutgoingP527_UsesParentStatementOrdinal()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(
                    ItemClaim("P527", "Q2", "2"),
                    ItemClaim("P527", "Q1", "1"))),
                ["Q1"] = Entity("Q1", "Book One"),
                ["Q2"] = Entity("Q2", "Book Two")
            });

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Equal(["Q1", "Q2"], manifest.Items.Select(i => i.Qid));
        Assert.Equal(["P527"], manifest.Items[0].SourceProperties);
        Assert.Contains(manifest.Items[0].Relationships, r => r.PropertyId == "P527" && r.TargetQid == "QSeries" && r.Direction == "Incoming");
    }

    [Fact]
    public async Task GetManifestAsync_ExpandsCollectionChildren()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(ItemClaim("P527", "QCollection", "1"))),
                ["QCollection"] = Entity("QCollection", "Omnibus", Claims(ItemClaim("P527", "QChild", "1.5"))),
                ["QChild"] = Entity("QChild", "Short Fiction")
            });

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Contains(manifest.Items, i => i.Qid == "QCollection" && i.IsCollection);
        var child = Assert.Single(manifest.Items, i => i.Qid == "QChild");
        Assert.True(child.IsExpandedFromCollection);
        Assert.Equal("QCollection", child.ParentCollectionQid);
        Assert.Equal("Omnibus", child.ParentCollectionLabel);
        Assert.Equal(SeriesManifestItemScope.CollectedContent, child.MembershipScope);
        Assert.Equal(
            SeriesManifestItemScope.MainSequence,
            Assert.Single(manifest.Items, i => i.Qid == "QCollection").MembershipScope);
    }

    [Fact]
    public async Task GetManifestAsync_ExpandedChildWithoutRootOrdinal_RemainsCollectedContent()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(ItemClaim("P527", "QVolume", "1"))),
                ["QVolume"] = Entity("QVolume", "Volume", Claims(ItemClaim("P527", "QNested", "2"))),
                ["QNested"] = Entity("QNested", "Nested work", Claims(ItemClaim("P179", "QSeries")))
            },
            p179: ["QNested"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        var nested = Assert.Single(manifest.Items, item => item.Qid == "QNested");
        Assert.Equal(SeriesManifestItemScope.CollectedContent, nested.MembershipScope);
        Assert.Equal("QVolume", nested.OrdinalScopeQid);
        Assert.Equal("2", nested.RawSeriesOrdinal);
    }

    [Fact]
    public async Task GetManifestAsync_ExpandedChildWithRootOrdinal_RemainsMainSequence()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(ItemClaim("P527", "QVolume", "1"))),
                ["QVolume"] = Entity("QVolume", "Volume", Claims(ItemClaim("P527", "QNested", "7"))),
                ["QNested"] = Entity("QNested", "Direct work", Claims(ItemClaim("P179", "QSeries", "2")))
            },
            p179: ["QNested"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        var nested = Assert.Single(manifest.Items, item => item.Qid == "QNested");
        Assert.Equal(SeriesManifestItemScope.MainSequence, nested.MembershipScope);
        Assert.Equal("QSeries", nested.OrdinalScopeQid);
        Assert.Equal("2", nested.RawSeriesOrdinal);
    }

    [Fact]
    public async Task GetManifestAsync_ClassifiesSameLabelMembersFromTheirOwnTypes()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["QBook"] = Entity("QBook", "Shared title", Claims(
                    ItemClaim("P179", "QSeries", "1"),
                    ItemClaim("P31", "Q571"))),
                ["QPlay"] = Entity("QPlay", "Shared title", Claims(
                    ItemClaim("P179", "QSeries"),
                    ItemClaim("P31", "Q25379")))
            },
            p179: ["QPlay", "QBook"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        var book = Assert.Single(manifest.Items, item => item.Qid == "QBook");
        Assert.Equal(SeriesManifestMediaKind.LiteraryWork, book.MediaKind);
        Assert.Equal(["Q571"], book.InstanceOfQids);

        var play = Assert.Single(manifest.Items, item => item.Qid == "QPlay");
        Assert.Equal(SeriesManifestMediaKind.StageWork, play.MediaKind);
        Assert.Equal(["Q25379"], play.InstanceOfQids);
    }

    [Fact]
    public async Task GetManifestAsync_IncludeCollectionsFalse_OmitsCollectionRows()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(ItemClaim("P527", "QCollection", "1"))),
                ["QCollection"] = Entity("QCollection", "Omnibus", Claims(ItemClaim("P527", "QChild", "1"))),
                ["QChild"] = Entity("QChild", "Short Fiction")
            });

        var manifest = await reconciler.Series.GetManifestAsync(new SeriesManifestRequest
        {
            SeriesQid = "QSeries",
            IncludeCollections = false
        });

        var child = Assert.Single(manifest.Items);
        Assert.Equal("QChild", child.Qid);
        Assert.True(child.IsExpandedFromCollection);
    }

    [Fact]
    public async Task GetManifestAsync_DecimalAndStringOrdinals_DoNotThrowAndSort()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(
                    ItemClaim("P527", "Q3", "special"),
                    ItemClaim("P527", "Q2", "1.5"),
                    ItemClaim("P527", "Q1", "0.1"))),
                ["Q1"] = Entity("Q1", "Point One"),
                ["Q2"] = Entity("Q2", "One Point Five"),
                ["Q3"] = Entity("Q3", "Special")
            });

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Equal(["Q1", "Q2", "Q3"], manifest.Items.Select(i => i.Qid));
        Assert.Equal(0.1m, manifest.Items[0].ParsedSeriesOrdinal);
        Assert.Equal(1.5m, manifest.Items[1].ParsedSeriesOrdinal);
        Assert.Null(manifest.Items[2].ParsedSeriesOrdinal);
        Assert.Equal("special", manifest.Items[2].RawSeriesOrdinal);
    }

    [Fact]
    public async Task GetManifestAsync_PreviousNextChain_OrdersWhenOrdinalsMissing()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["Q1"] = Entity("Q1", "First", Claims(ItemClaim("P179", "QSeries"), ItemClaim("P156", "Q2"))),
                ["Q2"] = Entity("Q2", "Second", Claims(ItemClaim("P179", "QSeries"), ItemClaim("P155", "Q1")))
            },
            p179: ["Q2", "Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Equal(["Q1", "Q2"], manifest.Items.Select(i => i.Qid));
        Assert.All(manifest.Items, item => Assert.Equal(SeriesManifestOrderSource.PreviousNextChain, item.OrderSource));
    }

    [Fact]
    public async Task GetManifestAsync_PublicationDateFallback_OrdersByP577()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["Q1"] = Entity("Q1", "Later", Claims(ItemClaim("P179", "QSeries"), DateClaim("P577", "+2020-01-01T00:00:00Z"))),
                ["Q2"] = Entity("Q2", "Earlier", Claims(ItemClaim("P179", "QSeries"), DateClaim("P577", "+2019-01-01T00:00:00Z")))
            },
            p179: ["Q1", "Q2"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Equal(["Q2", "Q1"], manifest.Items.Select(i => i.Qid));
        Assert.All(manifest.Items, item => Assert.Equal(SeriesManifestOrderSource.PublicationDate, item.OrderSource));
    }

    [Fact]
    public async Task GetManifestAsync_LabelFallback_AddsWarning()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["Q1"] = Entity("Q1", "Beta", Claims(ItemClaim("P179", "QSeries"))),
                ["Q2"] = Entity("Q2", "Alpha", Claims(ItemClaim("P179", "QSeries")))
            },
            p179: ["Q1", "Q2"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Equal(["Q2", "Q1"], manifest.Items.Select(i => i.Qid));
        Assert.Contains(manifest.Warnings, w => w.Code == "LabelFallbackOnly");
    }

    [Fact]
    public async Task GetManifestAsync_DuplicateAcrossSources_MergesProvenanceAndWarns()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(ItemClaim("P527", "Q1", "1"))),
                ["Q1"] = Entity("Q1", "Book One", Claims(ItemClaim("P179", "QSeries", "1")))
            },
            p179: ["Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        var item = Assert.Single(manifest.Items);
        Assert.Equal(["P179", "P527"], item.SourceProperties);
        Assert.Contains(manifest.Warnings, w => w.Code == "DuplicateItem" && w.Qid == "Q1");
    }

    [Fact]
    public async Task GetManifestAsync_ConflictingOrdinals_AddsWarning()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(ItemClaim("P527", "Q1", "2"))),
                ["Q1"] = Entity("Q1", "Book One", Claims(ItemClaim("P179", "QSeries", "1")))
            },
            p179: ["Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Contains(manifest.Warnings, w => w.Code == "ConflictingOrdinals" && w.Qid == "Q1");
    }

    [Fact]
    public async Task GetManifestAsync_BrokenPreviousNextChain_AddsWarning()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series"),
                ["Q1"] = Entity("Q1", "Book One", Claims(ItemClaim("P179", "QSeries"), ItemClaim("P156", "QMissing")))
            },
            p179: ["Q1"]);

        var manifest = await reconciler.Series.GetManifestAsync("QSeries");

        Assert.Contains(manifest.Warnings, w => w.Code == "BrokenPreviousNextChain" && w.Qid == "Q1");
    }

    [Fact]
    public async Task GetManifestAsync_MaxDepthAndMaxItems_AddWarnings()
    {
        using var reconciler = CreateReconciler(
            new()
            {
                ["QSeries"] = Entity("QSeries", "Series", Claims(
                    ItemClaim("P527", "QCollection", "1"),
                    ItemClaim("P527", "Q2", "2"),
                    ItemClaim("P527", "Q3", "3"))),
                ["QCollection"] = Entity("QCollection", "Collection", Claims(ItemClaim("P527", "QChild", "1"))),
                ["Q2"] = Entity("Q2", "Second"),
                ["Q3"] = Entity("Q3", "Third"),
                ["QChild"] = Entity("QChild", "Child")
            });

        var manifest = await reconciler.Series.GetManifestAsync(new SeriesManifestRequest
        {
            SeriesQid = "QSeries",
            MaxDepth = 1,
            MaxItems = 2
        });

        Assert.Equal(2, manifest.Items.Count);
        Assert.Contains(manifest.Warnings, w => w.Code == "MaxDepthReached" && w.Qid == "QCollection");
        Assert.Contains(manifest.Warnings, w => w.Code == "MaxItemsReached");
        Assert.Equal(SeriesManifestCompleteness.Truncated, manifest.Completeness);
    }

    private static WikidataReconciler CreateReconciler(
        Dictionary<string, Dictionary<string, object?>> entities,
        IReadOnlyList<string>? p179 = null,
        IReadOnlyList<string>? p361 = null,
        IReadOnlyList<string>? p8345 = null)
    {
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            var uri = Uri.UnescapeDataString(request.RequestUri!.ToString());

            if (uri.Contains("action=query", StringComparison.OrdinalIgnoreCase))
            {
                if (uri.Contains("haswbstatement:P179=", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.QueryResponse((p179 ?? []).ToArray())));
                if (uri.Contains("haswbstatement:P361=", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.QueryResponse((p361 ?? []).ToArray())));
                if (uri.Contains("haswbstatement:P8345=", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.QueryResponse((p8345 ?? []).ToArray())));
            }

            if (uri.Contains("action=wbgetentities", StringComparison.OrdinalIgnoreCase))
            {
                var ids = ParseIds(uri);
                var payloadEntities = ids
                    .Where(entities.ContainsKey)
                    .Select(id => entities[id])
                    .ToArray();
                return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.EntityResponse(payloadEntities)));
            }

            throw new InvalidOperationException($"Unexpected request: {uri}");
        });

        return TestPayloads.CreateReconciler(handler);
    }

    private static string[] ParseIds(string uri)
    {
        var match = Regex.Match(uri, @"[?&]ids=([^&]+)");
        return match.Success
            ? match.Groups[1].Value.Split('|', StringSplitOptions.RemoveEmptyEntries)
            : [];
    }

    private static Dictionary<string, object?> Entity(
        string id,
        string label,
        Dictionary<string, object>? claims = null)
        => TestPayloads.Entity(id, label, claims);

    private static Dictionary<string, object> Claims(params TestPayloads.ClaimSpec[] claims)
        => TestPayloads.ClaimsWithQualifiers(claims);

    private static TestPayloads.ClaimSpec ItemClaim(string propertyId, string targetQid, string? ordinal = null)
        => TestPayloads.Claim(
            propertyId,
            "wikibase-item",
            TestPayloads.ItemDataValue(targetQid),
            qualifiers: ordinal is null
                ? null
                : TestPayloads.Qualifiers(("P1545", "string", TestPayloads.StringDataValue(ordinal))));

    private static TestPayloads.ClaimSpec DateClaim(string propertyId, string time)
        => TestPayloads.Claim(propertyId, "time", TestPayloads.TimeDataValue(time));
}
