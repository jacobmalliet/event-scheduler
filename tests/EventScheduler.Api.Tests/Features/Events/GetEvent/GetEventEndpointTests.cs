using System.Net;
using System.Net.Http.Json;
using EventScheduler.Api.Features.Events.GetEvent;

namespace EventScheduler.Api.Tests.Features.Events.GetEvent;

public sealed class GetEventEndpointTests : ApiTestBase
{
    [Fact]
    public async Task GetEvent_ExistingEvent_ReturnsEventWithRegisteredCount()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 4);
        await RegisterAsync(createdEvent.Id, "ada@example.com");

        // Act
        var response = await Client.GetAsync($"/api/events/{createdEvent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetchedEvent = await response.Content.ReadFromJsonAsync<GetEventResponse>();
        Assert.Equal(createdEvent.Id, fetchedEvent!.Id);
        Assert.Equal(createdEvent.Title, fetchedEvent.Title);
        Assert.Equal(4, fetchedEvent.MaxCapacity);
        Assert.Equal(1, fetchedEvent.RegisteredCount);
    }

    [Fact]
    public async Task GetEvent_UnknownEvent_ReturnsNotFound()
    {
        // Arrange
        var unknownEventId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/events/{unknownEventId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("event_not_found", await response.ReadProblemCodeAsync());
    }
}
