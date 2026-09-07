using System.Net;
using Tuvima.Wikidata.Internal;

namespace Tuvima.Wikidata.Tests;

public class Phase1HttpReliabilityTests
{
    private const string Url = "https://www.wikidata.org/w/api.php?action=wbgetentities&ids=Q1&format=json";
    private const string Success = "{\"entities\":{},\"success\":1}";

    [Theory]
    [InlineData("{\"error\":{\"code\":\"maxlag\",\"info\":\"Busy\"}}")]
    [InlineData("{\"errors\":[{\"code\":\"ratelimited\",\"text\":\"Busy\"}]}")]
    public async Task ProviderBusyResponse_IsRetriedBeforeCaching(string error)
    {
        var calls = 0;
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
        {
            var response = TestHttpMessageHandler.Json(++calls == 1 ? error : Success);
            response.Headers.RetryAfter = new(TimeSpan.FromSeconds(5));
            return Task.FromResult(response);
        }));
        using var pipeline = new ResilientHttpClient(client, Options(retries: 1), new(), (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        });

        Assert.Equal(Success, await pipeline.GetStringAsync(Url, default));
        Assert.Equal(Success, await pipeline.GetStringAsync(Url, default));
        Assert.Equal(2, calls);
        Assert.Equal(TimeSpan.FromSeconds(5), Assert.Single(delays));
    }

    [Theory]
    [InlineData("{not json", "MalformedResponse")]
    [InlineData("null", "MalformedResponse")]
    [InlineData("{\"entities\":42}", "MalformedResponse")]
    [InlineData("{\"error\":{\"code\":\"badvalue\",\"info\":\"Invalid IDs\"}}", "ProviderRejected")]
    [InlineData("{\"error\":{\"code\":\"missingtitle\"}}", "NotFound")]
    public async Task InvalidResponse_IsTypedNotRetriedAndNotCached(string body, string kind)
    {
        var calls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(TestHttpMessageHandler.Json(++calls == 1 ? body : Success))));
        using var pipeline = new ResilientHttpClient(client, Options(retries: 2), new());

        var failure = await Assert.ThrowsAsync<WikidataProviderException>(() => pipeline.GetStringAsync(Url, default));
        Assert.Equal(kind, failure.Kind.ToString());
        Assert.Equal(1, calls);
        Assert.Equal(Success, await pipeline.GetStringAsync(Url, default));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ExhaustedMaxlag_IsRateLimitedAndNotCached()
    {
        var calls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(TestHttpMessageHandler.Json("{\"error\":{\"code\":\"maxlag\"}}"));
        }));
        using var pipeline = new ResilientHttpClient(client, Options(retries: 1), new(), (_, _) => Task.CompletedTask);
        for (var i = 0; i < 2; i++)
        {
            var failure = await Assert.ThrowsAsync<WikidataProviderException>(() => pipeline.GetStringAsync(Url, default));
            Assert.Equal(WikidataFailureKind.RateLimited, failure.Kind);
        }
        Assert.Equal(4, calls);
    }

    [Fact]
    public async Task CancelledCaller_DoesNotCancelOtherWaiter()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(async (_, token) =>
        {
            Interlocked.Increment(ref calls);
            started.TrySetResult();
            await release.Task.WaitAsync(token);
            return TestHttpMessageHandler.Json(Success);
        }));
        using var pipeline = new ResilientHttpClient(client, Options(), new());
        using var cancellation = new CancellationTokenSource();
        var first = pipeline.GetStringAsync(Url, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        var second = pipeline.GetStringAsync(Url, default);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        release.SetResult();
        Assert.Equal(Success, await second.WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CancelledFollower_DoesNotCancelOriginalCaller()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new TestHttpMessageHandler(async (_, token) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(token);
            return TestHttpMessageHandler.Json(Success);
        }));
        using var pipeline = new ResilientHttpClient(client, Options(), new());
        var first = pipeline.GetStringAsync(Url, default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        using var cancellation = new CancellationTokenSource();
        var second = pipeline.GetStringAsync(Url, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        release.SetResult();
        Assert.Equal(Success, await first.WaitAsync(TimeSpan.FromSeconds(3)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Dispose_CancelsPendingBodyWithoutDisposingAnActiveLease(bool coalescing)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
        {
            started.TrySetResult();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StalledContent() });
        }));
        using var pipeline = new ResilientHttpClient(client, new()
        {
            EnableRequestCoalescing = coalescing,
            WikidataRateLimit = ProviderRateLimitOptions.Unthrottled
        }, new());
        var task = pipeline.GetStringAsync(Url, default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        pipeline.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(3)));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pipeline.GetStringAsync(Url, default));
    }

    [Fact]
    public async Task OldPoisonedCacheEntry_IsIgnoredAndReplaced()
    {
        var options = Options();
        var cacheKey = ProviderRequest.Create(Url + "&maxlag=5").CacheKey;
        await options.ResponseCache!.SetAsync(cacheKey, "{\"error\":{\"code\":\"maxlag\"}}", TimeSpan.FromHours(1));
        var calls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(TestHttpMessageHandler.Json(Success));
        }));
        using var pipeline = new ResilientHttpClient(client, options, new());
        Assert.Equal(Success, await pipeline.GetStringAsync(Url, default));
        Assert.Equal(Success, await pipeline.GetStringAsync(Url, default));
        Assert.Equal(1, calls);
        Assert.Equal(Success, await options.ResponseCache.GetAsync(cacheKey));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task PermanentHttpRejection_IsNotRetried(HttpStatusCode status)
    {
        var calls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(TestHttpMessageHandler.Json("{}", status));
        }));
        using var pipeline = new ResilientHttpClient(client, Options(retries: 2), new());
        var failure = await Assert.ThrowsAsync<WikidataProviderException>(() => pipeline.GetStringAsync(Url, default));
        Assert.Equal(WikidataFailureKind.ProviderRejected, failure.Kind);
        Assert.Equal(status, failure.StatusCode);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SuppliedClientShorterTimeout_CoversBodyToo()
    {
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StalledContent() })))
        { Timeout = TimeSpan.FromMilliseconds(80) };
        using var pipeline = new ResilientHttpClient(client, Options(), new());
        var failure = await Assert.ThrowsAsync<WikidataProviderException>(() =>
            pipeline.GetStringAsync(Url, default).WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.Equal(WikidataFailureKind.TransientNetworkFailure, failure.Kind);
    }

    [Fact]
    public async Task LastWaiterCancellation_StopsWorkAndAllowsFreshRequest()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(async (_, token) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                started.SetResult();
                try { await Task.Delay(Timeout.Infinite, token); }
                finally { stopped.SetResult(); }
            }
            return TestHttpMessageHandler.Json(Success);
        }));
        using var pipeline = new ResilientHttpClient(client, Options(), new());
        using var cancellation = new CancellationTokenSource();
        var first = pipeline.GetStringAsync(Url, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(Success, await pipeline.GetStringAsync(Url, default).WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BodyTimeout_ReleasesHostSlot_AndCanRetry(bool retry)
    {
        var calls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler((_, _) =>
            Task.FromResult(++calls == 1
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StalledContent() }
                : TestHttpMessageHandler.Json(Success)))) { Timeout = Timeout.InfiniteTimeSpan };
        using var pipeline = new ResilientHttpClient(client, Options(retries: retry ? 1 : 0, timeout: TimeSpan.FromMilliseconds(80)), new(), (_, _) => Task.CompletedTask);
        if (retry)
            Assert.Equal(Success, await pipeline.GetStringAsync(Url, default).WaitAsync(TimeSpan.FromSeconds(3)));
        else
        {
            var failure = await Assert.ThrowsAsync<WikidataProviderException>(() =>
                pipeline.GetStringAsync(Url, default).WaitAsync(TimeSpan.FromSeconds(3)));
            Assert.Equal(WikidataFailureKind.TransientNetworkFailure, failure.Kind);
            Assert.Equal(Success, await pipeline.GetStringAsync(Url, default).WaitAsync(TimeSpan.FromSeconds(3)));
        }
        Assert.Equal(2, calls);
    }

    internal static WikidataReconcilerOptions Options(int retries = 0, TimeSpan? timeout = null) => new()
    {
        MaxRetries = retries,
        Timeout = timeout ?? TimeSpan.FromSeconds(5),
        RetryJitterRatio = 0,
        WikidataRateLimit = ProviderRateLimitOptions.Unthrottled with { MaxConcurrentRequests = 1 },
        DefaultRateLimit = ProviderRateLimitOptions.Unthrottled
    };

    private sealed class StalledContent : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => SerializeToStreamAsync(stream, context, default);
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
