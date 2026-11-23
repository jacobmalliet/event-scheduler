using EventScheduler.Api.Common;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Api.Features.Events.ListEvents;

/// <summary>
/// Query string for <c>GET /api/events</c>: <c>?page=2&amp;pageSize=10</c>. Pages are 1-based.
/// </summary>
public sealed record ListEventsRequest(int Page = 1, int PageSize = Paging.DefaultPageSize);

public sealed record ListEventsResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int MaxCapacity,
    int RegisteredCount);

/// <summary>
/// <c>GET /api/events</c>: one page of events, soonest first, each with its registration count.
/// </summary>
public sealed class ListEventsEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events", HandleAsync)
            .WithValidation<ListEventsRequest>()
            .WithName("ListEvents")
            .WithTags("Events");

    /// <summary>
    /// Reads one page of events, soonest first.
    /// </summary>
    /// <param name="request">The paging query string, already validated.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>200 OK with the page.</returns>
    internal static async Task<Ok<PagedResponse<ListEventsResponse>>> HandleAsync(
        [AsParameters] ListEventsRequest request,
        EventSchedulerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var totalCount = await dbContext.Events.CountAsync(cancellationToken);

        // Id breaks ties between events with the same start time, so pages never overlap or skip.
        var items = await dbContext.Events
            .AsNoTracking()
            .OrderBy(e => e.StartsAt)
            .ThenBy(e => e.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new ListEventsResponse(e.Id, e.Title, e.Description, e.StartsAt, e.EndsAt, e.MaxCapacity, e.Registrations.Count))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedResponse<ListEventsResponse>(items, request.Page, request.PageSize, totalCount));
    }
}
