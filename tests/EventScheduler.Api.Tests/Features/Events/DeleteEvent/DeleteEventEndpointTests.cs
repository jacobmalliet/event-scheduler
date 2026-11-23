using System.Net;

namespace EventScheduler.Api.Tests.Features.Events.DeleteEvent;

public sealed class DeleteEventEndpointTests : ApiTestBase
{
    [Fact]
    public async Task DeleteEvent_ExistingEvent_ReturnsNoContentAndRemovesEvent()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/events/{createdEvent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetchResponse = await Client.GetAsync($"/api/events/{createdEvent.Id}");
        Assert.Equal(HttpStatusCode.NotFound, fetchResponse.StatusCode);
        Assert.Empty((await ListEventsAsync()).Items);
    }

    [Fact]
    public async Task DeleteEvent_EventWithRegistrations_RemovesItsRegistrations()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        await RegisterAsync(createdEvent.Id, "ada@example.com");
        var otherEvent = await CreateEventAsync();
        await RegisterAsync(otherEvent.Id, "grace@example.com");

        // Act
        await Client.DeleteAsync($"/api/events/{createdEvent.Id}");

        // Assert
        var registrationsResponse = await Client.GetAsync($"/api/events/{createdEvent.Id}/registrations");
        Assert.Equal(HttpStatusCode.NotFound, registrationsResponse.StatusCode);
        var otherRegistration = Assert.Single((await ListRegistrationsAsync(otherEvent.Id)).Items);
        Assert.Equal("grace@example.com", otherRegistration.Email);
    }

    [Fact]
    public async Task DeleteEvent_UnknownEvent_ReturnsNotFound()
    {
        // Arrange
        var unknownEventId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/events/{unknownEventId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("event_not_found", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task DeleteEvent_PastEvent_ReturnsEventInPastConflict()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(startsIn: TimeSpan.FromHours(1));
        Factory.Clock.Advance(TimeSpan.FromHours(2));

        // Act
        var response = await Client.DeleteAsync($"/api/events/{createdEvent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("event_in_past", await response.ReadProblemCodeAsync());
    }
}
