using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tuvima.Wikidata;
using Tuvima.Wikidata.Graph;

// An offline comparison harness, not a substitute for production load testing.
const int iterations = 100;
const int samples = 7;
using var http = new HttpClient(new FixtureHandler());
using var reconciler = new WikidataReconciler(http, new()
{
    EnableResponseCaching = false,
    MaxConcurrency = 5,
    WikidataRateLimit = ProviderRateLimitOptions.Unthrottled
});
var bridgeRequests = Enumerable.Range(1, 20).Select(i => new BridgeResolutionRequest
{
    CorrelationKey = $"row{i}", MediaKind = BridgeMediaKind.Movie, Title = $"Example Movie {i}",
    BridgeIds = new Dictionary<string, string> { ["imdb_id"] = $"tt{i:D7}" }
}).ToArray();
var queries = Enumerable.Range(1, 20).Select(i => new ReconciliationRequest { Query = $"Q{i}" }).ToArray();
var nodes = Enumerable.Range(1, 1000).Select(i => new GraphNode { Qid = $"Q{i}" }).ToArray();
var edges = Enumerable.Range(2, 999).Select(i => new GraphEdge
    { SubjectQid = $"Q{i / 2}", ObjectQid = $"Q{i}", Relationship = "child" }).ToArray();
var graph = new EntityGraph(nodes, edges);

// Capture externally observable bridge results before timing. Exclude timing/cache telemetry.
var contracts = new List<object>();
foreach (var target in Enum.GetValues<BridgeRollupTarget>())
{
    var result = await reconciler.Bridge.ResolveAsync(new BridgeResolutionRequest
    {
        CorrelationKey = "rollup", MediaKind = BridgeMediaKind.Movie,
        Title = "Example Movie 1", RollupTarget = target,
        BridgeIds = new Dictionary<string, string> { ["imdb_id"] = "tt0000001" }
    });
    contracts.Add(Contract(result));
}
contracts.Add(Contract(await reconciler.Bridge.ResolveAsync(new BridgeResolutionRequest
    { CorrelationKey = "fallback", MediaKind = BridgeMediaKind.Movie, Title = "Example Movie 1" })));
var contractJson = JsonSerializer.Serialize(contracts);
var api = string.Join('\n', typeof(WikidataReconciler).Assembly.GetExportedTypes()
    .OrderBy(t => t.FullName, StringComparer.Ordinal)
    .SelectMany(t => new[] { t.FullName! }.Concat(t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Select(m => m.ToString()!).Order(StringComparer.Ordinal))));

var workloads = new (string Name, Func<Task<int>> Run)[]
{
    ("text-reconciliation", async () => (await reconciler.Reconcile.ReconcileAsync("Example Movie 1")).Count),
    ("reconciliation-batch-20", async () => (await reconciler.Reconcile.ReconcileBatchAsync(queries)).Count),
    ("bridge-id-rollup", async () => (await reconciler.Bridge.ResolveAsync(bridgeRequests[0])).Candidates.Count),
    ("bridge-batch-20", async () => (await reconciler.Bridge.ResolveBatchAsync(bridgeRequests)).Count),
    ("graph-path-1000-nodes", () => Task.FromResult(graph.FindPaths("Q1", "Q1000", 10).Count)),
    ("graph-build-1000-nodes", () => Task.FromResult(new EntityGraph(nodes, edges).EdgeCount))
};
var measurements = new List<object>();
foreach (var (name, run) in workloads)
{
    for (var i = 0; i < 30; i++) await run();
    var times = new List<double>();
    var allocations = new List<double>();
    long checksum = 0;
    for (var sample = 0; sample < samples; sample++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var bytes = GC.GetTotalAllocatedBytes(precise: true);
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++) checksum += await run();
        watch.Stop();
        allocations.Add((GC.GetTotalAllocatedBytes(precise: true) - bytes) / (double)iterations);
        times.Add(watch.Elapsed.TotalMilliseconds / iterations);
    }
    times.Sort(); allocations.Sort();
    measurements.Add(new { name, medianMilliseconds = times[samples / 2], minMilliseconds = times[0],
        maxMilliseconds = times[^1], medianAllocatedBytes = allocations[samples / 2], checksum });
}
var output = JsonSerializer.Serialize(new
{
    runtime = RuntimeInformation.FrameworkDescription, os = RuntimeInformation.OSDescription,
    architecture = RuntimeInformation.ProcessArchitecture.ToString(), iterations, samples,
    contractSha256 = Hash(contractJson), publicApiSha256 = Hash(api), contracts, measurements
}, new JsonSerializerOptions { WriteIndented = true });
if (args.Length > 0) await File.WriteAllTextAsync(args[0], output);
Console.WriteLine(output);

static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
static object Contract(BridgeResolutionResult result) => new
{
    result.CorrelationKey, result.Status, result.MatchedBy, result.SelectedCandidate, result.Candidates,
    result.Rollup, result.Series, result.Relationships, result.FailureKind, result.FailureMessage,
    result.Diagnostics.AttemptedStrategies, result.Diagnostics.MatchedProperties,
    result.Diagnostics.RejectedCandidates, result.Diagnostics.Warnings, result.Diagnostics.CompletedPhase
};

sealed class FixtureHandler : HttpMessageHandler
{
    private static readonly Dictionary<string, object> Entities = Enumerable.Range(1, 20)
        .SelectMany(i => new[] { (Id: $"Q{i}", Entity: Entity(i)), (Id: $"Q{1000 + i}", Entity: Related(1000 + i)) })
        .Append(("Q2001", Related(2001)))
        .ToDictionary(x => x.Item1, x => x.Item2, StringComparer.Ordinal);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = Uri.UnescapeDataString(request.RequestUri!.Query);
        object body;
        if (query.Contains("action=wbgetentities", StringComparison.Ordinal))
        {
            var ids = query.Split('&').Single(p => p.StartsWith("ids=", StringComparison.Ordinal))[4..].Split('|');
            body = new { entities = ids.Where(Entities.ContainsKey).ToDictionary(id => id, id => Entities[id]) };
        }
        else if (query.Contains("action=wbsearchentities", StringComparison.Ordinal))
            body = new { search = new[] { new { id = "Q1", label = "Example Movie 1" }, new { id = "Q2", label = "Example Movie 2" } } };
        else if (query.Contains("action=query", StringComparison.Ordinal))
        {
            var match = Regex.Match(query, @"tt(\d{7})");
            var ids = match.Success ? new[] { $"Q{int.Parse(match.Groups[1].Value)}" } : new[] { "Q1", "Q2" };
            body = new { query = new { search = ids.Select(id => new { title = id }).ToArray() } };
        }
        else throw new InvalidOperationException($"Unexpected benchmark request: {query}");
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") });
    }

    private static object Entity(int id) => new
    {
        id = $"Q{id}", type = "item", labels = new { en = new { language = "en", value = $"Example Movie {id}" } },
        claims = new Dictionary<string, object>
        {
            ["P31"] = Item("P31", "Q11424"), ["P345"] = Text("P345", $"tt{id:D7}"),
            ["P629"] = Item("P629", $"Q{1000 + id}"), ["P179"] = Item("P179", "Q2001"),
            ["P1545"] = Text("P1545", id.ToString())
        }
    };
    private static object Related(int id) => new
    {
        id = $"Q{id}", type = "item", labels = new { en = new { language = "en", value = $"Related {id}" } },
        claims = new Dictionary<string, object> { ["P31"] = Item("P31", id == 2001 ? "Q24856" : "Q11424") }
    };
    private static object Item(string property, string id) => new[] { new { rank = "normal", mainsnak = new
        { snaktype = "value", property, datatype = "wikibase-item", datavalue = new { type = "wikibase-entityid", value = new { id } } } } };
    private static object Text(string property, string value) => new[] { new { rank = "normal", mainsnak = new
        { snaktype = "value", property, datatype = "string", datavalue = new { type = "string", value } } } };
}
