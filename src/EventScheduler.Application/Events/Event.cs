using EventScheduler.Application.Registrations;

namespace EventScheduler.Application.Events;

/// <summary>
/// An event that attendees register for by email.
/// </summary>
public class Event
{
    /// <summary>
    /// The maximum length of <see cref="Title"/>, in characters.
    /// </summary>
    public const int TitleMaxLength = 200;

    /// <summary>
    /// The maximum length of <see cref="Description"/>, in characters.
    /// </summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>
    /// The smallest allowed <see cref="MaxCapacity"/>.
    /// </summary>
    public const int MaxCapacityLowerBound = 1;

    /// <summary>
    /// The largest allowed <see cref="MaxCapacity"/>.
    /// </summary>
    public const int MaxCapacityUpperBound = 10_000;

    /// <summary>
    /// Gets or sets the unique identifier of the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event's title: required, at most <see cref="TitleMaxLength"/> characters.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the optional description (at most <see cref="DescriptionMaxLength"/> characters),
    /// or <see langword="null"/> when there is none.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the start time in UTC. Once it has passed, the event can no longer be edited, deleted,
    /// registered for or unregistered from.
    /// </summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>
    /// Gets or sets the end time in UTC, which is always after <see cref="StartsAt"/>.
    /// </summary>
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of registrations, between <see cref="MaxCapacityLowerBound"/> and
    /// <see cref="MaxCapacityUpperBound"/>. It can't be lowered below the current registration count.
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Gets or sets the registrations for this event.
    /// </summary>
    public List<Registration> Registrations { get; set; } = [];

    /// <summary>
    /// Determines whether the event has started. A started event can no longer be edited, deleted,
    /// registered for or unregistered from.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns><see langword="true"/> if <see cref="StartsAt"/> is at or before <paramref name="now"/>.</returns>
    public bool HasStarted(DateTimeOffset now) => StartsAt <= now;

    /// <summary>
    /// Determines whether every seat is taken, so no one else can register.
    /// </summary>
    /// <param name="registeredCount">The number of current registrations.</param>
    /// <returns><see langword="true"/> if <paramref name="registeredCount"/> has reached <see cref="MaxCapacity"/>.</returns>
    public bool IsFull(int registeredCount) => registeredCount >= MaxCapacity;

    /// <summary>
    /// Determines whether <see cref="MaxCapacity"/> can be changed to <paramref name="newCapacity"/>
    /// without leaving more registrations than seats.
    /// </summary>
    /// <param name="newCapacity">The requested maximum capacity.</param>
    /// <param name="registeredCount">The number of current registrations.</param>
    /// <returns><see langword="true"/> if <paramref name="newCapacity"/> is at least <paramref name="registeredCount"/>.</returns>
    public bool CanChangeCapacityTo(int newCapacity, int registeredCount) => newCapacity >= registeredCount;
}
