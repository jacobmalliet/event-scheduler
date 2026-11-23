using System.Net;
using System.Net.Http.Json;
using EventScheduler.Api.Features.Events.GetEvent;
using EventScheduler.Api.Features.Events.UpdateEvent;
using Microsoft.AspNetCore.Mvc;

namespace EventScheduler.Api.Tests.Features.Events.UpdateEvent;

public sealed class UpdateEventEndpointTests : ApiTestBase
{
    [Fact]
    public async Task UpdateEvent_ValidRequest_ReturnsAndPersistsChanges()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 5);
        var newStartsAt = Factory.Clock.GetUtcNow().AddDays(30);
        var newEndsAt = newStartsAt.AddDays(1);
        var request = new UpdateEventRequest("  Renamed  ", "New description", newStartsAt, newEndsAt, 8);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/events/{createdEvent.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedEvent = await response.Content.ReadFromJsonAsync<UpdateEventResponse>();
        Assert.Equal("Renamed", updatedEvent!.Title);
        Assert.Equal(8, updatedEvent.MaxCapacity);

        var fetchedEvent = await Client.GetFromJsonAsync<GetEventResponse>($"/api/events/{createdEvent.Id}");
        Assert.Equal("Renamed", fetchedEvent!.Title);
        Assert.Equal("New description", fetchedEvent.Description);
        Assert.Equal(newStartsAt, fetchedEvent.StartsAt);
        Assert.Equal(newEndsAt, fetchedEvent.EndsAt);
        Assert.Equal(8, fetchedEvent.MaxCapacity);
    }

    [Fact]
    public async Task UpdateEvent_InvalidPayload_ReturnsValidationProblem()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        var request = new UpdateEventRequest("", null, Factory.Clock.GetUtcNow().AddDays(-1), Factory.Clock.GetUtcNow(), 0);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/events/{createdEvent.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Title", problem!.Errors.Keys);
        Assert.Contains("StartsAt", problem.Errors.Keys);
        Assert.Contains("MaxCapacity", problem.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEvent_EndsBeforeStart_ReturnsValidationProblem()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        var startsAt = Factory.Clock.GetUtcNow().AddDays(14);
        var request = new UpdateEventRequest("Updated event", null, startsAt, startsAt.AddHours(-1), 5);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/events/{createdEvent.Id}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["EndsAt"], problem!.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEvent_UnknownEvent_ReturnsNotFound()
    {
        // Arrange
        var unknownEventId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/events/{unknownEventId}", ValidUpdate(maxCapacity: 5));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("event_not_found", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UpdateEvent_CapacityBelowRegistrations_ReturnsCapacityBelowRegistrationsConflict()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 5);
        await RegisterAsync(createdEvent.Id, "first@example.com");
        await RegisterAsync(createdEvent.Id, "second@example.com");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/events/{createdEvent.Id}", ValidUpdate(maxCapacity: 1));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("capacity_below_registrations", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UpdateEvent_CapacityEqualToRegistrations_ReturnsOk()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 5);
        await RegisterAsync(createdEvent.Id, "first@example.com");
        await RegisterAsync(createdEvent.Id, "second@example.com");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/events/{createdEvent.Id}", ValidUpdate(maxCapacity: 2));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedEvent = await response.Content.ReadFromJsonAsync<UpdateEventResponse>();
        Assert.Equal(2, updatedEvent!.MaxCapacity);
        Assert.Equal(2, updatedEvent.RegisteredCount);
    }

    [Fact]
    public async Task UpdateEvent_PastEvent_ReturnsEventInPastConflict()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(startsIn: TimeSpan.FromHours(1));
        Factory.Clock.Advance(TimeSpan.FromHours(2));

        // Act
        var response = await Client.PutAsJsonAsync($"/api/events/{createdEvent.Id}", ValidUpdate(maxCapacity: 5));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("event_in_past", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UpdateEvent_LowerCapacityDuringConcurrentRegistrations_NeverExceedsCapacity()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 10);

        // Act
        var registrationRequests = Enumerable.Range(0, 10)
            .Select(i => RegisterAsync(createdEvent.Id, $"user{i}@example.com"));
        var updateRequest = Client.PutAsJsonAsync($"/api/events/{createdEvent.Id}", ValidUpdate(maxCapacity: 3));
        await Task.WhenAll(registrationRequests.Append(updateRequest));

        // Assert
        // Depending on ordering the update either succeeds (capacity 3) or is rejected because more than
        // 3 people had already registered; either way the event must never be over capacity.
        var fetchedEvent = await Client.GetFromJsonAsync<GetEventResponse>($"/api/events/{createdEvent.Id}");
        Assert.True(
            fetchedEvent!.RegisteredCount <= fetchedEvent.MaxCapacity,
            $"{fetchedEvent.RegisteredCount} registrations exceed capacity {fetchedEvent.MaxCapacity}.");
    }

    private UpdateEventRequest ValidUpdate(int maxCapacity) =>
        new(
            "Updated event",
            "Updated description",
            Factory.Clock.GetUtcNow().AddDays(14),
            Factory.Clock.GetUtcNow().AddDays(15),
            maxCapacity);
}
