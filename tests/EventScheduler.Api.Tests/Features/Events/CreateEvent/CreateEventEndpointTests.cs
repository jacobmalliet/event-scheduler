using System.Net;
using System.Net.Http.Json;
using EventScheduler.Api.Features.Events.CreateEvent;
using EventScheduler.Application.Events;
using Microsoft.AspNetCore.Mvc;

namespace EventScheduler.Api.Tests.Features.Events.CreateEvent;

public sealed class CreateEventEndpointTests : ApiTestBase
{
    [Fact]
    public async Task CreateEvent_ValidRequest_AppearsInEventList()
    {
        // Arrange
        const int MaxCapacity = 5;

        // Act
        var createdEvent = await CreateEventAsync(MaxCapacity);

        // Assert
        var events = await ListEventsAsync();
        var listedEvent = Assert.Single(events.Items);
        Assert.Equal(createdEvent.Id, listedEvent.Id);
        Assert.Equal("Test event", listedEvent.Title);
        Assert.Equal(createdEvent.StartsAt, listedEvent.StartsAt);
        Assert.Equal(createdEvent.EndsAt, listedEvent.EndsAt);
        Assert.Equal(MaxCapacity, listedEvent.MaxCapacity);
        Assert.Equal(0, listedEvent.RegisteredCount);
    }

    [Fact]
    public async Task CreateEvent_ValidRequest_ReturnsCreatedWithLocation()
    {
        // Arrange
        var request = new CreateEventRequest("Launch", null, Factory.Clock.GetUtcNow().AddDays(1), Factory.Clock.GetUtcNow().AddDays(2), 3);

        // Act
        var response = await Client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();
        Assert.Equal($"/api/events/{body!.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task CreateEvent_TitleAtLimitWithSurroundingSpaces_ReturnsCreatedWithTrimmedTitle()
    {
        // Arrange
        var title = new string('a', Event.TitleMaxLength);
        var request = new CreateEventRequest($"  {title}  ", null, Factory.Clock.GetUtcNow().AddDays(1), Factory.Clock.GetUtcNow().AddDays(2), 3);

        // Act
        var response = await Client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();
        Assert.Equal(title, body!.Title);
    }

    [Fact]
    public async Task CreateEvent_TitleAndDescriptionOverLimit_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CreateEventRequest(
            new string('a', Event.TitleMaxLength + 1), new string('b', Event.DescriptionMaxLength + 1), Factory.Clock.GetUtcNow().AddDays(1), Factory.Clock.GetUtcNow().AddDays(2), 3);

        // Act
        var response = await Client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Title", problem!.Errors.Keys);
        Assert.Contains("Description", problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateEvent_InvalidPayload_ReturnsValidationProblem()
    {
        // Arrange
        var request = new CreateEventRequest("", null, Factory.Clock.GetUtcNow().AddDays(-1), Factory.Clock.GetUtcNow(), 0);

        // Act
        var response = await Client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Title", problem!.Errors.Keys);
        Assert.Contains("StartsAt", problem.Errors.Keys);
        Assert.Contains("MaxCapacity", problem.Errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateEvent_EndsAtOrBeforeStart_ReturnsValidationProblem(int endOffsetHours)
    {
        // Arrange
        var startsAt = Factory.Clock.GetUtcNow().AddDays(1);
        var request = new CreateEventRequest("Launch", null, startsAt, startsAt.AddHours(endOffsetHours), 3);

        // Act
        var response = await Client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["EndsAt"], problem!.Errors.Keys);
    }
}
