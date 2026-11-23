using EventScheduler.Api.Common;
using EventScheduler.Infrastructure.Concurrency;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Api.Features.Events.UpdateEvent;

public sealed record UpdateEventRequest(string Title, string? Description, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int MaxCapacity);

public sealed record UpdateEventResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int MaxCapacity,
    int RegisteredCount);

/// <summary>
/// <c>PUT /api/events/{eventId}</c>: replaces an event's details.
/// Rejected once the event has started, or if the new capacity is below the current number of registrations.
/// </summary>
public sealed class UpdateEventEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}", HandleAsync)
            .WithValidation<UpdateEventRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("UpdateEvent")
            .WithTags("Events");

    /// <summary>
    /// Checks the business rules and applies the update while holding the event's lock.
    /// </summary>
    /// <param name="eventId">The event's id, from the route.</param>
    /// <param name="request">The request body, already validated.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="clock">Supplies the current time.</param>
    /// <param name="eventLocks">The per-event locks that serialize changes to an event.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>200 OK with the updated event, or a 404 or 409 problem.</returns>
    internal static async Task<Results<Ok<UpdateEventResponse>, ProblemHttpResult>> HandleAsync(
        Guid eventId,
        UpdateEventRequest request,
        EventSchedulerDbContext dbContext,
        TimeProvider clock,
        KeyedLock<Guid> eventLocks,
        CancellationToken cancellationToken)
    {
        // Same lock as registrations: lowering capacity must not race a registration taking a seat.
        using var eventLock = await eventLocks.AcquireAsync(eventId, cancellationToken);

        var entity = await dbContext.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (entity is null)
        {
            return Errors.EventNotFound.ToProblem();
        }

        if (entity.HasStarted(clock.GetUtcNow()))
        {
            return Errors.EventInPast.ToProblem();
        }

        var registeredCount = await dbContext.Registrations.CountAsync(r => r.EventId == eventId, cancellationToken);
        if (!entity.CanChangeCapacityTo(request.MaxCapacity, registeredCount))
        {
            return Errors.CapacityBelowRegistrations.ToProblem();
        }

        entity.Title = request.Title.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.StartsAt = request.StartsAt.ToUniversalTime();
        entity.EndsAt = request.EndsAt.ToUniversalTime();
        entity.MaxCapacity = request.MaxCapacity;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new UpdateEventResponse(
            entity.Id, entity.Title, entity.Description, entity.StartsAt, entity.EndsAt, entity.MaxCapacity, registeredCount));
    }

    /// <summary>
    /// The business-rule failures this endpoint can return.
    /// </summary>
    internal static class Errors
    {
        /// <summary>
        /// No event exists with the given id.
        /// </summary>
        public static readonly Error EventNotFound = Error.NotFound("event_not_found", "Event not found.");

        /// <summary>
        /// The event has already started.
        /// </summary>
        public static readonly Error EventInPast = Error.Conflict("event_in_past", "Cannot edit an event that has already started.");

        /// <summary>
        /// The new capacity is lower than the number of current registrations.
        /// </summary>
        public static readonly Error CapacityBelowRegistrations = Error.Conflict(
            "capacity_below_registrations", "Max capacity cannot be lower than the number of current registrations.");
    }
}
