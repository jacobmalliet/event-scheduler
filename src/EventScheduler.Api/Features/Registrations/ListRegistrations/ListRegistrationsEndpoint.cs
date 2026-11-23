using EventScheduler.Api.Common;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Api.Features.Registrations.ListRegistrations;

/// <summary>
/// Query string for <c>GET /api/events/{eventId}/registrations</c>: <c>?page=2&amp;pageSize=10</c>. Pages are 1-based.
/// </summary>
public sealed record ListRegistrationsRequest(int Page = 1, int PageSize = Paging.DefaultPageSize);

public sealed record ListRegistrationsResponse(Guid Id, string Email, DateTimeOffset RegisteredAt);

/// <summary>
/// <c>GET /api/events/{eventId}/registrations</c>: one page of the event's attendees, in the order they registered.
/// </summary>
public sealed class ListRegistrationsEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}/registrations", HandleAsync)
            .WithValidation<ListRegistrationsRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithName("ListRegistrations")
            .WithTags("Registrations");

    /// <summary>
    /// Reads one page of the event's registrations, in the order they were made.
    /// </summary>
    /// <param name="eventId">The event's id, from the route.</param>
    /// <param name="request">The paging query string, already validated.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>200 OK with the page of attendees, or a 404 problem.</returns>
    internal static async Task<Results<Ok<PagedResponse<ListRegistrationsResponse>>, ProblemHttpResult>> HandleAsync(
        Guid eventId,
        [AsParameters] ListRegistrationsRequest request,
        EventSchedulerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Events.AnyAsync(e => e.Id == eventId, cancellationToken))
        {
            return Errors.EventNotFound.ToProblem();
        }

        var registrations = dbContext.Registrations.Where(r => r.EventId == eventId);
        var totalCount = await registrations.CountAsync(cancellationToken);

        // Id breaks ties between registrations made at the same instant, so pages never overlap or skip.
        var items = await registrations
            .AsNoTracking()
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ListRegistrationsResponse(r.Id, r.Email, r.RegisteredAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(
            new PagedResponse<ListRegistrationsResponse>(items, request.Page, request.PageSize, totalCount));
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
