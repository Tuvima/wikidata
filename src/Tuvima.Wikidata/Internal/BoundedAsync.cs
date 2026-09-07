using System.Runtime.CompilerServices;

namespace Tuvima.Wikidata.Internal;

/// <summary>A bounded window of work, with backpressure and operation-owned cleanup.</summary>
internal static class BoundedAsync
{
    public static async Task<TResult[]> SelectAsync<T, TResult>(
        IReadOnlyList<T> items, int concurrency, Func<T, CancellationToken, Task<TResult>> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = new TResult[items.Count];
        await foreach (var item in StreamAsync(items, concurrency, action, cancellationToken).ConfigureAwait(false))
            results[item.Index] = item.Result;
        return results;
    }

    public static async IAsyncEnumerable<(int Index, TResult Result)> StreamAsync<T, TResult>(
        IReadOnlyList<T> items, int concurrency, Func<T, CancellationToken, Task<TResult>> action,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pending = new List<Task<(int Index, TResult Result)>>();
        var next = 0;
        var limit = Math.Clamp(concurrency, 1, 1024);
        try
        {
            while (next < items.Count || pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (next < items.Count && pending.Count < limit)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    pending.Add(RunAsync(next++));
                }
                var completed = await Task.WhenAny(pending).ConfigureAwait(false);
                pending.Remove(completed);
                yield return await completed.ConfigureAwait(false);
            }
        }
        finally
        {
            lifetime.Cancel();
            // Observe every outstanding operation, including on early enumerator disposal.
            try { await Task.WhenAll(pending).ConfigureAwait(false); }
            catch { /* The primary failure/cancellation remains with the consumer. */ }
        }

        async Task<(int Index, TResult Result)> RunAsync(int index)
            => (index, await action(items[index], lifetime.Token).ConfigureAwait(false));
    }
}
