using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Tuvima.WikidataReconciliation.AspNetCore;

/// <summary>
/// Extension methods to map W3C Reconciliation Service API endpoints.
/// Compatible with OpenRefine, Google Sheets reconciliation, and any W3C-compatible client.
/// </summary>
public static class ReconciliationEndpoints
{
    /// <summary>
    /// Maps the W3C Reconciliation Service API endpoints at the specified path prefix.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pathPrefix">The URL prefix (e.g., "/api/reconcile"). Default is "/reconcile".</param>
    /// <param name="configure">Optional configuration for the service manifest.</param>
    public static IEndpointRouteBuilder MapReconciliation(
        this IEndpointRouteBuilder endpoints,
        string pathPrefix = "/reconcile",
        Action<ReconciliationServiceOptions>? configure = null)
    {
        var serviceOptions = new ReconciliationServiceOptions();
        configure?.Invoke(serviceOptions);

        var prefix = pathPrefix.TrimEnd('/');

        // GET — returns service manifest
        endpoints.MapGet(prefix, (HttpContext ctx) =>
        {
            var manifest = BuildManifest(serviceOptions, ctx.Request);
            return Results.Json(manifest, W3cJsonContext.Default.ServiceManifest);
        });

        // POST — reconciliation queries
        endpoints.MapPost(prefix, async (HttpContext ctx, WikidataReconciler reconciler) =>
        {
            var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);

            // Check for batch queries (W3C spec: queries parameter as JSON)
            if (form.TryGetValue("queries", out var queriesJson) && !string.IsNullOrEmpty(queriesJson))
            {
                var queries = JsonSerializer.Deserialize(queriesJson!, W3cJsonContext.Default.DictionaryStringW3cQuery);
                if (queries is null)
                    return Results.BadRequest("Invalid queries parameter");

                var results = new Dictionary<string, List<W3cCandidate>>();

                foreach (var (key, query) in queries)
                {
                    var request = MapToRequest(query);
                    var reconciled = await reconciler.ReconcileAsync(request, ctx.RequestAborted);
                    results[key] = reconciled.Select(MapToCandidate).ToList();
                }

                return Results.Json(results, W3cJsonContext.Default.DictionaryStringListW3cCandidate);
            }

            // Single query via "query" parameter
            if (form.TryGetValue("query", out var queryParam) && !string.IsNullOrEmpty(queryParam))
            {
                var query = JsonSerializer.Deserialize(queryParam!, W3cJsonContext.Default.W3cQuery);
                if (query is null)
                    return Results.BadRequest("Invalid query parameter");

                var request = MapToRequest(query);
                var reconciled = await reconciler.ReconcileAsync(request, ctx.RequestAborted);
                var response = new W3cQueryResponse { Result = reconciled.Select(MapToCandidate).ToList() };
                return Results.Json(response, W3cJsonContext.Default.W3cQueryResponse);
            }

            return Results.BadRequest("Missing 'queries' or 'query' parameter");
        });

        // GET with queries parameter (JSONP support for OpenRefine)
        endpoints.MapGet($"{prefix}/{{**catchall}}", (HttpContext ctx, WikidataReconciler reconciler) =>
        {
            // If no path segments, the base GET handler above handles it
            return Results.NotFound();
        });

        return endpoints;
    }

    private static ServiceManifest BuildManifest(ReconciliationServiceOptions options, HttpRequest request)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}";

        return new ServiceManifest
        {
            Name = options.ServiceName,
            IdentifierSpace = options.IdentifierSpace,
            SchemaSpace = options.SchemaSpace,
            View = new ServiceView { Url = options.EntityViewUrl },
            DefaultTypes = options.DefaultTypes.Select(t => new W3cType { Id = t.Id, Name = t.Name }).ToList()
        };
    }

    private static ReconciliationRequest MapToRequest(W3cQuery query)
    {
        var request = new ReconciliationRequest
        {
            Query = query.Query ?? "",
            Type = query.Type,
            Limit = query.Limit > 0 ? query.Limit : 5
        };

        if (query.Properties is { Count: > 0 })
        {
            var props = query.Properties
                .Where(p => !string.IsNullOrEmpty(p.Pid) && !string.IsNullOrEmpty(p.V))
                .Select(p => new PropertyConstraint(p.Pid!, p.V!))
                .ToList();

            if (props.Count > 0)
                return new ReconciliationRequest
                {
                    Query = request.Query,
                    Type = request.Type,
                    Limit = request.Limit,
                    Properties = props
                };
        }

        return request;
    }

    private static W3cCandidate MapToCandidate(ReconciliationResult result) => new()
    {
        Id = result.Id,
        Name = result.Name,
        Description = result.Description,
        Score = result.Score,
        Match = result.Match,
        Type = result.Types?.Select(t => new W3cType { Id = t }).ToList() ?? []
    };
}

// ─── W3C Models ─────────────────────────────────────────────────

internal sealed class ServiceManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("identifierSpace")]
    public string IdentifierSpace { get; set; } = "";

    [JsonPropertyName("schemaSpace")]
    public string SchemaSpace { get; set; } = "";

    [JsonPropertyName("view")]
    public ServiceView? View { get; set; }

    [JsonPropertyName("defaultTypes")]
    public List<W3cType> DefaultTypes { get; set; } = [];
}

internal sealed class ServiceView
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}

internal sealed class W3cQuery
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("properties")]
    public List<W3cPropertyValue>? Properties { get; set; }
}

internal sealed class W3cPropertyValue
{
    [JsonPropertyName("pid")]
    public string? Pid { get; set; }

    [JsonPropertyName("v")]
    public string? V { get; set; }
}

internal sealed class W3cQueryResponse
{
    [JsonPropertyName("result")]
    public List<W3cCandidate> Result { get; set; } = [];
}

internal sealed class W3cCandidate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("match")]
    public bool Match { get; set; }

    [JsonPropertyName("type")]
    public List<W3cType> Type { get; set; } = [];
}

internal sealed class W3cType
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

[JsonSerializable(typeof(ServiceManifest))]
[JsonSerializable(typeof(W3cQueryResponse))]
[JsonSerializable(typeof(W3cQuery))]
[JsonSerializable(typeof(Dictionary<string, W3cQuery>))]
[JsonSerializable(typeof(Dictionary<string, List<W3cCandidate>>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class W3cJsonContext : JsonSerializerContext;
