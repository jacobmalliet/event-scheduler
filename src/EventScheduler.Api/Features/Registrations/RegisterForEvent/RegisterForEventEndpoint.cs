using EventScheduler.Api.Common;
using EventScheduler.Application.Registrations;
using EventScheduler.Infrastructure.Concurrency;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Api.Features.Registrations.RegisterForEvent;

public sealed record RegisterForEventRequest(string Email);

public sealed record RegisterForEventResponse(Guid Id, Guid EventId, string Email, DateTimeOffset RegisteredAt);

/// <summary>
/// <c>POST /api/events/{eventId}/registrations</c>: registers an email for an event.
/// Enforces the business rules: the event hasn't started, the email isn't already registered
/// (compared after normalizing), and the event isn't at capacity.
/// </summary>
public sealed class RegisterForEventEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/registrations", HandleAsync)
            .WithValidation<RegisterForEventRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithName("RegisterForEvent")
            .WithTags("Registrations");

    /// <summary>
    /// Checks the business rules and adds the registration while holding the event's lock.
    /// </summary>
    /// <param name="eventId">The event's id, from the route.</param>
    /// <param name="request">The request body, already validated.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="clock">Supplies the current time.</param>
    /// <param name="eventLocks">The per-event locks that serialize changes to an event.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>201 Created with the registration, or a 404 or 409 problem.</returns>
    internal static async Task<Results<Created<RegisterForEventResponse>, ProblemHttpResult>> HandleAsync(
        Guid eventId,
        RegisterForEventRequest request,
        EventSchedulerDbContext dbContext,
        TimeProvider clock,
        KeyedLock<Guid> eventLocks,
        CancellationToken cancellationToken)
    {
        var email = Registration.NormalizeEmail(request.Email);

        // Hold the event's lock from the rule checks through the insert, so concurrent
        // registrations can't both see a free seat (see KeyedLock remarks).
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

        var registrations = dbContext.Registrations.Where(r => r.EventId == eventId);

        if (await registrations.AnyAsync(r => r.Email == email, cancellationToken))
        {
            return Errors.AlreadyRegistered.ToProblem();
        }

        var currentRegistrationCount = await registrations.CountAsync(cancellationToken);
        if (entity.IsFull(currentRegistrationCount))
        {
            return Errors.EventFull.ToProblem();
        }

        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Email = email,
            RegisteredAt = clock.GetUtcNow()
        };

        dbContext.Registrations.Add(registration);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new RegisterForEventResponse(registration.Id, registration.EventId, registration.Email, registration.RegisteredAt);
        return TypedResults.Created($"/api/events/{eventId}/registrations/{registration.Id}", response);
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
        public static readonly Error EventInPast = Error.Conflict("event_in_past", "Cannot register for a past event.");

        /// <summary>
        /// The email is already registered for the event.
        /// </summary>
        public static readonly Error AlreadyRegistered = Error.Conflict("already_registered", "This email is already registered for the event.");

        /// <summary>
        /// The event has no seats left.
        /// </summary>
        public static readonly Error EventFull = Error.Conflict("event_full", "Event is at capacity.");
    }
}
