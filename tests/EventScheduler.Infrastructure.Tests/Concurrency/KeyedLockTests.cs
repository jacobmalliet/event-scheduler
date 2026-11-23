using EventScheduler.Infrastructure.Concurrency;

namespace EventScheduler.Infrastructure.Tests.Concurrency;

public sealed class KeyedLockTests
{
    private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(5);

    private readonly KeyedLock<int> _keyedLock = new();

    [Fact]
    public async Task AcquireAsync_SameKeyHeld_WaitsUntilReleased()
    {
        // Arrange
        var firstHolder = await _keyedLock.AcquireAsync(1, CancellationToken.None);

        // Act
        var secondAcquire = _keyedLock.AcquireAsync(1, CancellationToken.None);
        var blockedWhileHeld = !secondAcquire.IsCompleted;
        firstHolder.Dispose();

        // Assert
        Assert.True(blockedWhileHeld);
        using var secondHolder = await secondAcquire.WaitAsync(_lockTimeout);
    }

    [Fact]
    public async Task AcquireAsync_KeysOnDifferentStripes_DoNotBlockEachOther()
    {
        // Arrange
        using var firstHolder = await _keyedLock.AcquireAsync(0, CancellationToken.None);

        // Act
        var otherAcquire = _keyedLock.AcquireAsync(1, CancellationToken.None);

        // Assert
        Assert.True(otherAcquire.IsCompletedSuccessfully);
        (await otherAcquire).Dispose();
    }

    [Fact]
    public async Task AcquireAsync_KeysOnSameStripe_AreSerialized()
    {
        // Arrange
        const int Key = 0;
        const int CollidingKey = Key + KeyedLock<int>.StripeCount;
        var firstHolder = await _keyedLock.AcquireAsync(Key, CancellationToken.None);

        // Act
        var collidingAcquire = _keyedLock.AcquireAsync(CollidingKey, CancellationToken.None);
        var blockedWhileHeld = !collidingAcquire.IsCompleted;
        firstHolder.Dispose();

        // Assert
        Assert.True(blockedWhileHeld);
        using var collidingHolder = await collidingAcquire.WaitAsync(_lockTimeout);
    }

    [Fact]
    public async Task AcquireAsync_HolderDisposedTwice_ReleasesOnlyOnce()
    {
        // Arrange
        var holder = await _keyedLock.AcquireAsync(1, CancellationToken.None);
        holder.Dispose();

        // Act
        holder.Dispose();

        // Assert
        using var nextHolder = await _keyedLock.AcquireAsync(1, CancellationToken.None);
        var anotherAcquire = _keyedLock.AcquireAsync(1, CancellationToken.None);
        Assert.False(anotherAcquire.IsCompleted);
    }

    [Fact]
    public async Task AcquireAsync_CancelledWhileWaiting_ThrowsOperationCanceled()
    {
        // Arrange
        using var holder = await _keyedLock.AcquireAsync(1, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var waitingAcquire = _keyedLock.AcquireAsync(1, cancellation.Token);

        // Act
        await cancellation.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingAcquire);
    }
}
