using System.Net;
using System.Net.Http.Json;
using EventScheduler.Api.Features.Registrations.RegisterForEvent;
using Microsoft.AspNetCore.Mvc;

namespace EventScheduler.Api.Tests.Features.Registrations.RegisterForEvent;

public sealed class RegisterForEventEndpointTests : ApiTestBase
{
    [Fact]
    public async Task RegisterForEvent_ValidEmail_AddsNormalizedAttendee()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();

        // Act
        var response = await RegisterAsync(createdEvent.Id, "  Ada@Example.com ");

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var registration = Assert.Single((await ListRegistrationsAsync(createdEvent.Id)).Items);
        Assert.Equal("ada@example.com", registration.Email);

        var events = await ListEventsAsync();
        Assert.Equal(1, Assert.Single(events.Items).RegisteredCount);
    }

    [Fact]
    public async Task RegisterForEvent_ValidEmail_ReturnsCreatedWithRegistrationLocation()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();

        // Act
        var response = await RegisterAsync(createdEvent.Id, "ada@example.com");

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterForEventResponse>();
        Assert.Equal($"/api/events/{createdEvent.Id}/registrations/{body!.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task RegisterForEvent_SameEmailDifferentCase_ReturnsAlreadyRegisteredConflict()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        await RegisterAsync(createdEvent.Id, "ada@example.com");

        // Act
        var response = await RegisterAsync(createdEvent.Id, "ADA@example.com");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("already_registered", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task RegisterForEvent_EventAtCapacity_ReturnsEventFullConflict()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 1);
        await RegisterAsync(createdEvent.Id, "first@example.com");

        // Act
        var response = await RegisterAsync(createdEvent.Id, "second@example.com");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("event_full", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task RegisterForEvent_PastEvent_ReturnsEventInPastConflict()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(startsIn: TimeSpan.FromHours(1));
        Factory.Clock.Advance(TimeSpan.FromHours(2));

        // Act
        var response = await RegisterAsync(createdEvent.Id, "late@example.com");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("event_in_past", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task RegisterForEvent_UnknownEvent_ReturnsNotFound()
    {
        // Arrange
        var unknownEventId = Guid.NewGuid();

        // Act
        var response = await RegisterAsync(unknownEventId, "ada@example.com");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("event_not_found", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task RegisterForEvent_InvalidEmail_ReturnsValidationProblem()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();

        // Act
        var response = await RegisterAsync(createdEvent.Id, "not-an-email");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Email", problem!.Errors.Keys);
    }

    [Fact]
    public async Task RegisterForEvent_ConcurrentRequestsBeyondCapacity_RegistersExactlyCapacity()
    {
        // Arrange
        const int Capacity = 5;
        var createdEvent = await CreateEventAsync(maxCapacity: Capacity);

        // Act
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(i => RegisterAsync(createdEvent.Id, $"user{i}@example.com")));

        // Assert
        // Exactly capacity succeeds; every other request is a clean "event_full" conflict,
        // and nothing beyond the successful registrations is persisted.
        Assert.Equal(Capacity, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        foreach (var rejected in responses.Where(r => r.StatusCode != HttpStatusCode.Created))
        {
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            Assert.Equal("event_full", await rejected.ReadProblemCodeAsync());
        }

        Assert.Equal(Capacity, (await ListRegistrationsAsync(createdEvent.Id)).TotalCount);
    }
}
