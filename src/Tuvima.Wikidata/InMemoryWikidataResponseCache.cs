namespace Tuvima.Wikidata;

/// <summary>
/// Bounded process-local LRU response cache. Applications can replace it with a durable cache.
/// Limits account for UTF-16 key and response payload bytes, excluding object overhead.
/// </summary>
public sealed class InMemoryWikidataResponseCache : IWikidataResponseCache
{
    private const long DefaultMaxSizeBytes = 64 * 1024 * 1024;
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _recency = new();
    private readonly SortedSet<Entry> _expiry = new(Comparer<Entry>.Create((a, b) =>
    {
        var order = a.ExpiresAt.CompareTo(b.ExpiresAt);
        return order != 0 ? order : StringComparer.Ordinal.Compare(a.Key, b.Key);
    }));
    private readonly int _maxEntries;
    private readonly long _maxSizeBytes;
    private readonly TimeProvider _timeProvider;
    private long _sizeBytes;

    /// <summary>Creates a cache limited to 1,024 entries and 64 MiB of string payloads.</summary>
    public InMemoryWikidataResponseCache() : this(1024, DefaultMaxSizeBytes) { }

    /// <summary>Creates a cache with positive entry and UTF-16 payload-byte limits.</summary>
    public InMemoryWikidataResponseCache(int maxEntries, long maxSizeBytes = DefaultMaxSizeBytes)
        : this(maxEntries, maxSizeBytes, TimeProvider.System) { }

    internal InMemoryWikidataResponseCache(int maxEntries, long maxSizeBytes, TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEntries);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSizeBytes);
        _maxEntries = maxEntries;
        _maxSizeBytes = maxSizeBytes;
        _timeProvider = timeProvider;
    }

    internal (int Count, long SizeBytes) GetUsage()
    {
        lock (_gate) return (_entries.Count, _sizeBytes);
    }

    public ValueTask<string?> GetAsync(WikidataResponseCacheKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            PruneExpired(_timeProvider.GetUtcNow());
            if (!_entries.TryGetValue(key.Key, out var entry))
                return ValueTask.FromResult<string?>(null);
            _recency.Remove(entry.Node);
            _recency.AddLast(entry.Node);
            return ValueTask.FromResult<string?>(entry.Response);
        }
    }

    public ValueTask SetAsync(
        WikidataResponseCacheKey key,
        string response,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(response);
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            PruneExpired(now);
            if (_entries.TryGetValue(key.Key, out var previous))
                Remove(previous);

            var size = 2L * (key.Key.Length + (long)response.Length);
            if (ttl <= TimeSpan.Zero || size > _maxSizeBytes)
                return ValueTask.CompletedTask;

            while (_entries.Count >= _maxEntries || _sizeBytes > _maxSizeBytes - size)
                Remove(_entries[_recency.First!.Value]);

            var expires = ttl >= DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now.Add(ttl);
            var entry = new Entry(key.Key, response, expires, size, _recency.AddLast(key.Key));
            _entries.Add(key.Key, entry);
            _expiry.Add(entry);
            _sizeBytes += size;
        }
        return ValueTask.CompletedTask;
    }

    private void PruneExpired(DateTimeOffset now)
    {
        while (_expiry.Min is { } entry && entry.ExpiresAt <= now)
            Remove(entry);
    }

    private void Remove(Entry entry)
    {
        _entries.Remove(entry.Key);
        _expiry.Remove(entry);
        _recency.Remove(entry.Node);
        _sizeBytes -= entry.Size;
    }

    private sealed record Entry(string Key, string Response, DateTimeOffset ExpiresAt,
        long Size, LinkedListNode<string> Node);
}
