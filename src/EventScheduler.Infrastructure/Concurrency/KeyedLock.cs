namespace EventScheduler.Infrastructure.Concurrency;

/// <summary>
/// In-process async lock keyed by id (e.g. an event id), used to serialize check-then-write operations.
/// </summary>
/// <remarks>
/// <para>
/// Locks are not reentrant. Never acquire a second key while holding one: two keys can share a stripe,
/// and waiting on a stripe you already hold deadlocks.
/// </para>
/// </remarks>
/// <typeparam name="TKey">The type of key to lock on, e.g. <see cref="Guid"/> for event ids.</typeparam>
public sealed class KeyedLock<TKey> where TKey : notnull
{
    /// <summary>
    /// The number of semaphores keys are spread across. A power of two, so a key's stripe is a bit mask of its hash.
    /// </summary>
    public const int StripeCount = 64;

    private readonly SemaphoreSlim[] _stripes =
        [.. Enumerable.Range(0, StripeCount).Select(_ => new SemaphoreSlim(1, 1))];

    /// <summary>
    /// Waits until the lock for <paramref name="key"/> is free, then takes it.
    /// </summary>
    /// <param name="key">The key to lock, e.g. an event id.</param>
    /// <param name="cancellationToken">Cancels the wait; the lock is not taken if cancelled.</param>
    /// <returns>A handle that releases the lock when disposed. Disposing it more than once is safe.</returns>
    public async Task<IDisposable> AcquireAsync(TKey key, CancellationToken cancellationToken)
    {
        var stripe = _stripes[GetStripeIndex(key)];
        await stripe.WaitAsync(cancellationToken);
        return new Releaser(stripe);
    }

    private static int GetStripeIndex(TKey key) => EqualityComparer<TKey>.Default.GetHashCode(key) & (StripeCount - 1);

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                semaphore.Release();
            }
        }
    }
}
