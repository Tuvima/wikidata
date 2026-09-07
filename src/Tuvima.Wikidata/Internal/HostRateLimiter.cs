namespace Tuvima.Wikidata.Internal;

internal sealed class HostRateLimiter : IDisposable
{
    private readonly SemaphoreSlim _concurrency;
    private readonly TimeSpan _minInterval;
    private readonly WikidataDiagnostics _diagnostics;
    private readonly SemaphoreSlim _pacing = new(1, 1);
    private readonly TimeProvider _timeProvider;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private long? _lastStart;

    public HostRateLimiter(ProviderRateLimitOptions options, WikidataDiagnostics diagnostics,
        TimeProvider? timeProvider = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var maxConcurrent = Math.Clamp(options.MaxConcurrentRequests, 1, 1024);
        _concurrency = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        _minInterval = options.RequestsPerSecond <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(1 / options.RequestsPerSecond);
        _diagnostics = diagnostics;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _delay = delay ?? Task.Delay;
    }

    public async ValueTask<IDisposable> WaitAsync(CancellationToken cancellationToken)
    {
        await _concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_minInterval > TimeSpan.Zero)
            {
                await _pacing.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    // Pace admissions after concurrency becomes available, using monotonic time.
                    while (_lastStart is { } last)
                    {
                        var wait = _minInterval - _timeProvider.GetElapsedTime(last);
                        if (wait <= TimeSpan.Zero) break;
                        _diagnostics.RecordThrottledWait(wait);
                        await _delay(wait, cancellationToken).ConfigureAwait(false);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    _lastStart = _timeProvider.GetTimestamp();
                }
                finally { _pacing.Release(); }
            }
            return new Lease(_concurrency);
        }
        catch { _concurrency.Release(); throw; }
    }

    public void Dispose()
    {
        _concurrency.Dispose();
        _pacing.Dispose();
    }

    private sealed class Lease : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public Lease(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _semaphore.Release();
        }
    }
}
