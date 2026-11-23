using System.Net;

namespace EventScheduler.Api.Tests.Features.Registrations.UnregisterFromEvent;

public sealed class UnregisterFromEventEndpointTests : ApiTestBase
{
    [Fact]
    public async Task UnregisterFromEvent_ExistingRegistration_ReturnsNoContentAndRemovesAttendee()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        var registration = await RegisterAttendeeAsync(createdEvent.Id, "ada@example.com");
        await RegisterAttendeeAsync(createdEvent.Id, "grace@example.com");

        // Act
        var response = await UnregisterAsync(createdEvent.Id, registration.Id);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = Assert.Single((await ListRegistrationsAsync(createdEvent.Id)).Items);
        Assert.Equal("grace@example.com", remaining.Email);
    }

    [Fact]
    public async Task UnregisterFromEvent_EmailWithSlash_RemovesRegistration()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        var registration = await RegisterAttendeeAsync(createdEvent.Id, "a/b@example.com");

        // Act
        var response = await UnregisterAsync(createdEvent.Id, registration.Id);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await ListRegistrationsAsync(createdEvent.Id)).Items);
    }

    [Fact]
    public async Task UnregisterFromEvent_UnknownRegistration_ReturnsRegistrationNotFound()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        var unknownRegistrationId = Guid.NewGuid();

        // Act
        var response = await UnregisterAsync(createdEvent.Id, unknownRegistrationId);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("registration_not_found", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UnregisterFromEvent_RegistrationOfOtherEvent_ReturnsRegistrationNotFound()
    {
        // Arrange
        var firstEvent = await CreateEventAsync();
        var secondEvent = await CreateEventAsync();
        var registration = await RegisterAttendeeAsync(firstEvent.Id, "ada@example.com");

        // Act
        var response = await UnregisterAsync(secondEvent.Id, registration.Id);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("registration_not_found", await response.ReadProblemCodeAsync());
        Assert.Single((await ListRegistrationsAsync(firstEvent.Id)).Items);
    }

    [Fact]
    public async Task UnregisterFromEvent_UnknownEvent_ReturnsEventNotFound()
    {
        // Arrange
        var unknownEventId = Guid.NewGuid();
        var unknownRegistrationId = Guid.NewGuid();

        // Act
        var response = await UnregisterAsync(unknownEventId, unknownRegistrationId);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("event_not_found", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UnregisterFromEvent_PastEvent_ReturnsEventInPastConflict()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(startsIn: TimeSpan.FromHours(1));
        var registration = await RegisterAttendeeAsync(createdEvent.Id, "ada@example.com");
        Factory.Clock.Advance(TimeSpan.FromHours(2));

        // Act
        var response = await UnregisterAsync(createdEvent.Id, registration.Id);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("event_in_past", await response.ReadProblemCodeAsync());
    }

    [Fact]
    public async Task UnregisterFromEvent_FullEvent_FreesSeatForNewRegistration()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 1);
        var registration = await RegisterAttendeeAsync(createdEvent.Id, "first@example.com");

        // Act
        await UnregisterAsync(createdEvent.Id, registration.Id);

        // Assert
        var response = await RegisterAsync(createdEvent.Id, "second@example.com");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
