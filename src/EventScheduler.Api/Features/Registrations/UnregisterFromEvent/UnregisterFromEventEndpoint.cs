using EventScheduler.Api.Common;
using EventScheduler.Infrastructure.Concurrency;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Api.Features.Registrations.UnregisterFromEvent;

/// <summary>
/// <c>DELETE /api/events/{eventId}/registrations/{registrationId}</c>: removes a registration.
/// Rejected once the event has started.
/// </summary>
public sealed class UnregisterFromEventEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/events/{eventId:guid}/registrations/{registrationId:guid}", HandleAsync)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("UnregisterFromEvent")
            .WithTags("Registrations");

    /// <summary>
    /// Checks the business rules and removes the registration while holding the event's lock.
    /// </summary>
    /// <param name="eventId">The event's id, from the route.</param>
    /// <param name="registrationId">The registration's id, from the route.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="clock">Supplies the current time.</param>
    /// <param name="eventLocks">The per-event locks that serialize changes to an event.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>204 No Content, or a 404 or 409 problem.</returns>
    internal static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid eventId,
        Guid registrationId,
        EventSchedulerDbContext dbContext,
        TimeProvider clock,
        KeyedLock<Guid> eventLocks,
        CancellationToken cancellationToken)
    {
        // Every change to an event's registrations is serialized on the event's lock.
        using var eventLock = await eventLocks.AcquireAsync(eventId, cancellationToken);

        var entity = await dbContext.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (entity is null)
        {
            return Errors.EventNotFound.ToProblem();
        }

        // Attendance history for events that have started is kept as-is.
        if (entity.HasStarted(clock.GetUtcNow()))
        {
            return Errors.EventInPast.ToProblem();
        }

        var registration = await dbContext.Registrations
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.EventId == eventId, cancellationToken);
        if (registration is null)
        {
            return Errors.RegistrationNotFound.ToProblem();
        }

        dbContext.Registrations.Remove(registration);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
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
        /// No registration with the given id exists for the event.
        /// </summary>
        public static readonly Error RegistrationNotFound = Error.NotFound(
            "registration_not_found", "Registration not found.");

        /// <summary>
        /// The event has already started.
        /// </summary>
        public static readonly Error EventInPast = Error.Conflict("event_in_past", "Cannot unregister from a past event.");
    }
}
