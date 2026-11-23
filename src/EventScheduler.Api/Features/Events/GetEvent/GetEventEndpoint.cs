using EventScheduler.Api.Common;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Api.Features.Events.GetEvent;

public sealed record GetEventResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int MaxCapacity,
    int RegisteredCount);

/// <summary>
/// <c>GET /api/events/{eventId}</c>: one event with its registration count.
/// </summary>
public sealed class GetEventEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}", HandleAsync)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithName("GetEvent")
            .WithTags("Events");

    /// <summary>
    /// Reads one event and its registration count.
    /// </summary>
    /// <param name="eventId">The event's id, from the route.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>200 OK with the event, or a 404 problem.</returns>
    internal static async Task<Results<Ok<GetEventResponse>, ProblemHttpResult>> HandleAsync(
        Guid eventId,
        EventSchedulerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var response = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => new GetEventResponse(e.Id, e.Title, e.Description, e.StartsAt, e.EndsAt, e.MaxCapacity, e.Registrations.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? Errors.EventNotFound.ToProblem()
            : TypedResults.Ok(response);
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
    }
}
