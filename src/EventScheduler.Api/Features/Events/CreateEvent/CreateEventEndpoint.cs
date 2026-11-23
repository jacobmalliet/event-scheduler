using EventScheduler.Api.Common;
using EventScheduler.Application.Events;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EventScheduler.Api.Features.Events.CreateEvent;

public sealed record CreateEventRequest(string Title, string? Description, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int MaxCapacity);

public sealed record CreateEventResponse(Guid Id, string Title, string? Description, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int MaxCapacity);

/// <summary>
/// <c>POST /api/events</c>: creates an event and returns it with a <c>Location</c> header.
/// Title and description are trimmed (a blank description is stored as null) and the start and end times are stored in UTC.
/// </summary>
public sealed class CreateEventEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events", HandleAsync)
            .WithValidation<CreateEventRequest>()
            .WithName("CreateEvent")
            .WithTags("Events");

    /// <summary>
    /// Creates the event.
    /// </summary>
    /// <param name="request">The request body, already validated.</param>
    /// <param name="dbContext">The database context.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>201 Created with the new event and its <c>Location</c>.</returns>
    internal static async Task<Created<CreateEventResponse>> HandleAsync(
        CreateEventRequest request,
        EventSchedulerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var entity = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            StartsAt = request.StartsAt.ToUniversalTime(),
            EndsAt = request.EndsAt.ToUniversalTime(),
            MaxCapacity = request.MaxCapacity
        };

        dbContext.Events.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateEventResponse(entity.Id, entity.Title, entity.Description, entity.StartsAt, entity.EndsAt, entity.MaxCapacity);
        return TypedResults.Created($"/api/events/{response.Id}", response);
    }
}
