using EventScheduler.Application.Events;

namespace EventScheduler.Application.Tests.Events;

public sealed class EventTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static Event CreateEvent(DateTimeOffset? startsAt = null, int maxCapacity = 10) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Conference",
        StartsAt = startsAt ?? _now.AddDays(1),
        MaxCapacity = maxCapacity
    };

    [Fact]
    public void HasStarted_StartsInFuture_ReturnsFalse()
    {
        // Arrange
        var upcomingEvent = CreateEvent(startsAt: _now.AddMinutes(1));

        // Act
        var hasStarted = upcomingEvent.HasStarted(_now);

        // Assert
        Assert.False(hasStarted);
    }

    [Fact]
    public void HasStarted_StartsExactlyNow_ReturnsTrue()
    {
        // Arrange
        var startingEvent = CreateEvent(startsAt: _now);

        // Act
        var hasStarted = startingEvent.HasStarted(_now);

        // Assert
        Assert.True(hasStarted);
    }

    [Fact]
    public void HasStarted_StartedInPast_ReturnsTrue()
    {
        // Arrange
        var pastEvent = CreateEvent(startsAt: _now.AddDays(-1));

        // Act
        var hasStarted = pastEvent.HasStarted(_now);

        // Assert
        Assert.True(hasStarted);
    }

    [Fact]
    public void HasStarted_SameInstantInOtherTimeZone_ReturnsTrue()
    {
        // Arrange
        var startingEvent = CreateEvent(startsAt: _now);
        var nowInOtherZone = _now.ToOffset(TimeSpan.FromHours(-5));

        // Act
        var hasStarted = startingEvent.HasStarted(nowInOtherZone);

        // Assert
        Assert.True(hasStarted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    public void IsFull_FewerRegistrationsThanCapacity_ReturnsFalse(int registeredCount)
    {
        // Arrange
        var openEvent = CreateEvent(maxCapacity: 10);

        // Act
        var isFull = openEvent.IsFull(registeredCount);

        // Assert
        Assert.False(isFull);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public void IsFull_RegistrationsAtOrAboveCapacity_ReturnsTrue(int registeredCount)
    {
        // Arrange
        var fullEvent = CreateEvent(maxCapacity: 10);

        // Act
        var isFull = fullEvent.IsFull(registeredCount);

        // Assert
        Assert.True(isFull);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    [InlineData(1, 0)]
    public void CanChangeCapacityTo_CapacityAtOrAboveRegistrations_ReturnsTrue(int newCapacity, int registeredCount)
    {
        // Arrange
        var existingEvent = CreateEvent(maxCapacity: 10);

        // Act
        var canChange = existingEvent.CanChangeCapacityTo(newCapacity, registeredCount);

        // Assert
        Assert.True(canChange);
    }

    [Fact]
    public void CanChangeCapacityTo_CapacityBelowRegistrations_ReturnsFalse()
    {
        // Arrange
        var existingEvent = CreateEvent(maxCapacity: 10);

        // Act
        var canChange = existingEvent.CanChangeCapacityTo(newCapacity: 4, registeredCount: 5);

        // Assert
        Assert.False(canChange);
    }
}
