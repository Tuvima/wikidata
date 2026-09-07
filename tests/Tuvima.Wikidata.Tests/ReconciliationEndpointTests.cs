using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Tuvima.Wikidata.AspNetCore;

namespace Tuvima.Wikidata.Tests;

public class ReconciliationEndpointTests
{
    [Theory]
    [InlineData("query", "{bad")]
    [InlineData("query", "null")]
    [InlineData("query", "[]")]
    [InlineData("query", "{}")]
    [InlineData("query", "{\"query\":\"   \"}")]
    [InlineData("query", "{\"query\":123}")]
    [InlineData("query", "{\"query\":\"Q1\",\"limit\":\"five\"}")]
    [InlineData("query", "{\"query\":\"Q1\",\"limit\":-1}")]
    [InlineData("query", "{\"query\":\"Q1\",\"properties\":[null]}")]
    [InlineData("query", "{\"query\":\"Q1\",\"properties\":[{}]}")]
    [InlineData("queries", "{\"ok\":{\"query\":\"Q1\"},\"bad\":null}")]
    [InlineData("queries", "{\"ok\":{\"query\":\"Q1\"},\"bad\":{\"query\":\" \"}}")]
    [InlineData("queries", "null")]
    [InlineData("queries", "{}")]
    [InlineData("queries", "[]")]
    public async Task InvalidPayload_Returns400WithoutProviderRequests(string field, string json)
    {
        await using var host = await EndpointHost.StartAsync();
        using var response = await host.Client.PostAsync("/reconcile", Form(field, json));
        await AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Empty(host.Provider.RequestedUris);
    }

    [Theory]
    [InlineData("query", "{\"query\":\"too long\"}")]
    [InlineData("query", "{\"query\":\"Q1\",\"limit\":3}")]
    [InlineData("query", "{\"query\":\"Q1\",\"properties\":[{\"pid\":\"P1\",\"v\":\"x\"},{\"pid\":\"P2\",\"v\":\"y\"}]}")]
    [InlineData("queries", "{\"a\":{\"query\":\"Q1\"},\"b\":{\"query\":\"Q2\"}}")]
    public async Task ConfiguredLimits_AreCheckedBeforeAnyProviderWork(string field, string json)
    {
        await using var host = await EndpointHost.StartAsync(options =>
        {
            options.MaxQueryLength = 4;
            options.MaxBatchSize = 1;
            options.MaxResultLimit = 2;
            options.MaxPropertiesPerQuery = 1;
        });
        using var response = await host.Client.PostAsync("/reconcile", Form(field, json));
        await AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Empty(host.Provider.RequestedUris);
    }

    [Fact]
    public async Task MissingDuplicateOrAmbiguousParameters_Return400()
    {
        await using var host = await EndpointHost.StartAsync();
        foreach (var pairs in new[]
        {
            Array.Empty<KeyValuePair<string, string>>(),
            new[] { KeyValuePair.Create("query", "{}"), KeyValuePair.Create("queries", "{}") },
            new[] { KeyValuePair.Create("query", "{}"), KeyValuePair.Create("query", "{}") }
        })
        {
            using var response = await host.Client.PostAsync("/reconcile", new FormUrlEncodedContent(pairs));
            await AssertProblem(response, HttpStatusCode.BadRequest);
        }
        Assert.Empty(host.Provider.RequestedUris);
    }

    [Fact]
    public async Task UnsupportedContentType_Returns415()
    {
        await using var host = await EndpointHost.StartAsync();
        using var response = await host.Client.PostAsync("/reconcile", new StringContent("{}", Encoding.UTF8, "application/json"));
        await AssertProblem(response, HttpStatusCode.UnsupportedMediaType);
        Assert.Empty(host.Provider.RequestedUris);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BodyLimit_AppliesWithAndWithoutContentLength(bool unknownLength)
    {
        await using var host = await EndpointHost.StartAsync(options => options.MaxRequestBodyBytes = 32);
        using HttpContent content = unknownLength
            ? new UnknownLengthForm(new string('x', 100))
            : new StringContent(new string('x', 100), Encoding.UTF8, "application/x-www-form-urlencoded");
        using var response = await host.Client.PostAsync("/reconcile", content);
        await AssertProblem(response, HttpStatusCode.RequestEntityTooLarge);
        Assert.Empty(host.Provider.RequestedUris);
    }

    [Fact]
    public async Task MalformedMultipart_Returns400()
    {
        await using var host = await EndpointHost.StartAsync();
        using var content = new StringContent("broken form");
        content.Headers.ContentType = new("multipart/form-data");
        using var response = await host.Client.PostAsync("/reconcile", content);
        await AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.Empty(host.Provider.RequestedUris);
    }

    [Fact]
    public async Task ValidSingleQuery_PreservesResponseShapeAndLanguage()
    {
        await using var host = await EndpointHost.StartAsync();
        host.Client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr");
        using var response = await host.Client.PostAsync("/reconcile", Form("query", "{\"query\":\"Q1\"}"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Q1", json.RootElement.GetProperty("result")[0].GetProperty("id").GetString());
        Assert.Equal("Exemple", json.RootElement.GetProperty("result")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task ValidBatch_PreservesCorrelationKeys()
    {
        await using var host = await EndpointHost.StartAsync();
        using var response = await host.Client.PostAsync("/reconcile", Form("queries", "{\"second\":{\"query\":\"Q2\"},\"first\":{\"query\":\"Q1\"}}"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Q2", json.RootElement.GetProperty("second")[0].GetProperty("id").GetString());
        Assert.Equal("Q1", json.RootElement.GetProperty("first")[0].GetProperty("id").GetString());
    }

    [Theory]
    [InlineData("entity")]
    [InlineData("property")]
    [InlineData("type")]
    public async Task Suggest_RejectsOversizedPrefix_ButPreservesEmptyResponse(string kind)
    {
        await using var host = await EndpointHost.StartAsync(options => options.MaxQueryLength = 3);
        using var oversized = await host.Client.GetAsync($"/reconcile/suggest/{kind}?prefix=long");
        await AssertProblem(oversized, HttpStatusCode.BadRequest);
        using var empty = await host.Client.GetAsync($"/reconcile/suggest/{kind}");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Empty(host.Provider.RequestedUris);
    }

    [Fact]
    public async Task ValidMultipartQuery_AtConfiguredTextAndBatchLimits_Succeeds()
    {
        await using var host = await EndpointHost.StartAsync(options =>
        {
            options.MaxBatchSize = 1;
            options.MaxQueryLength = 2;
            options.MaxResultLimit = 1;
        });
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("{\"query\":\"Q1\",\"limit\":1}"), "query");
        using var response = await host.Client.PostAsync("/reconcile", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task InvalidLimitConfiguration_FailsAtEndpointMapping(int setting)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => EndpointHost.StartAsync(options =>
        {
            switch (setting)
            {
                case 0: options.MaxBatchSize = 0; break;
                case 1: options.MaxQueryLength = 0; break;
                case 2: options.MaxResultLimit = 0; break;
                case 3: options.MaxPropertiesPerQuery = 0; break;
                case 4: options.MaxRequestBodyBytes = 0; break;
            }
        }));
    }

    [Fact]
    public async Task ClientCancellation_CancelsProviderWork()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = await EndpointHost.StartAsync(handler: async (_, token) =>
        {
            started.SetResult();
            try { await Task.Delay(Timeout.Infinite, token); }
            finally { stopped.SetResult(); }
            return TestHttpMessageHandler.Json("{}");
        });
        using var cancellation = new CancellationTokenSource();
        var response = host.Client.PostAsync("/reconcile", Form("query", "{\"query\":\"Q1\"}"), cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => response.WaitAsync(TimeSpan.FromSeconds(5)));
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static FormUrlEncodedContent Form(string field, string json)
        => new([KeyValuePair.Create(field, json)]);

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)expected, json.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class EndpointHost(WebApplication app, HttpClient client, HttpClient providerClient, TestHttpMessageHandler provider) : IAsyncDisposable
    {
        public HttpClient Client => client;
        public TestHttpMessageHandler Provider => provider;
        public static async Task<EndpointHost> StartAsync(Action<ReconciliationServiceOptions>? configure = null,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handler = null)
        {
            var provider = new TestHttpMessageHandler(handler ?? ((request, _) =>
            {
                var qid = request.RequestUri!.Query.Contains("ids=Q2", StringComparison.Ordinal) ? "Q2" : "Q1";
                var entity = TestPayloads.Entity(qid, "Example");
                entity["labels"] = TestPayloads.Labels(("en", "Example"), ("fr", "Exemple"));
                return Task.FromResult(TestHttpMessageHandler.Json(TestPayloads.EntityResponse(entity)));
            }));
            var providerClient = new HttpClient(provider);
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton(_ => new WikidataReconciler(providerClient, new()
            {
                WikidataRateLimit = ProviderRateLimitOptions.Unthrottled
            }));
            var app = builder.Build();
            try
            {
                app.MapReconciliation(configure: configure);
                await app.StartAsync();
                return new(app, app.GetTestClient(), providerClient, provider);
            }
            catch
            {
                await app.DisposeAsync();
                providerClient.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await app.DisposeAsync();
            providerClient.Dispose();
        }
    }

    private sealed class UnknownLengthForm : HttpContent
    {
        private readonly string _body;
        public UnknownLengthForm(string body)
        {
            _body = body;
            Headers.ContentType = new("application/x-www-form-urlencoded");
        }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => stream.WriteAsync(Encoding.UTF8.GetBytes(_body)).AsTask();
    }
}
