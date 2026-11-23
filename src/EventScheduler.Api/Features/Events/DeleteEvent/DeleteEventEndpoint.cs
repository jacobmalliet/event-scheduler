using EventScheduler.Api.Common;
using EventScheduler.Infrastructure.Concurrency;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Api.Features.Events.DeleteEvent;

/// <summary>
/// <c>DELETE /api/events/{eventId}</c>: deletes an event and its registrations. Rejected once the event has started.
/// </summary>
public sealed class DeleteEventEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/events/{eventId:guid}", HandleAsync)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("DeleteEvent")
            .WithTags("Events");

    /// <summary>
    /// Checks the business rules and deletes the event and its registrations while holding the event's lock.
    /// </summary>
    /// <param name="eventId">The event's id, from the route.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="clock">Supplies the current time.</param>
    /// <param name="eventLocks">The per-event locks that serialize changes to an event.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>204 No Content, or a 404 or 409 problem.</returns>
    internal static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid eventId,
        EventSchedulerDbContext dbContext,
        TimeProvider clock,
        KeyedLock<Guid> eventLocks,
        CancellationToken cancellationToken)
    {
        // Same lock as registrations, so a registration can't land on an event mid-delete.
        using var eventLock = await eventLocks.AcquireAsync(eventId, cancellationToken);

        // Registrations are loaded so EF removes them too: InMemory has no database-level cascade,
        // EF only cascades to entities it is tracking.
        var entity = await dbContext.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (entity is null)
        {
            return Errors.EventNotFound.ToProblem();
        }

        // Started events keep their attendance history.
        if (entity.HasStarted(clock.GetUtcNow()))
        {
            return Errors.EventInPast.ToProblem();
        }

        dbContext.Events.Remove(entity);
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
        /// The event has already started.
        /// </summary>
        public static readonly Error EventInPast = Error.Conflict("event_in_past", "Cannot delete an event that has already started.");
    }
}
