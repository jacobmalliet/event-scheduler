using System.Net;
using System.Net.Http.Json;
using EventScheduler.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventScheduler.Api.Tests.Features.Registrations.ListRegistrations;

public sealed class ListRegistrationsEndpointTests : ApiTestBase
{
    [Fact]
    public async Task ListRegistrations_ExistingEvent_ReturnsAttendeesInRegistrationOrder()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        await RegisterAsync(createdEvent.Id, "grace@example.com");
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await RegisterAsync(createdEvent.Id, "ada@example.com");

        // Act
        var page = await ListRegistrationsAsync(createdEvent.Id);

        // Assert
        Assert.Equal(["grace@example.com", "ada@example.com"], page.Items.Select(r => r.Email));
        Assert.Equal(1, page.Page);
        Assert.Equal(Paging.DefaultPageSize, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task ListRegistrations_MoreAttendeesThanOnePage_ReturnsPagesOf10InOrderWithoutOverlap()
    {
        // Arrange
        var createdEvent = await CreateEventAsync(maxCapacity: 25);
        var emails = new List<string>();
        for (var attendee = 1; attendee <= 25; attendee++)
        {
            var email = $"attendee{attendee:D2}@example.com";
            emails.Add(email);
            await RegisterAsync(createdEvent.Id, email);
            Factory.Clock.Advance(TimeSpan.FromSeconds(1));
        }

        // Act
        var pages = new[]
        {
            await ListRegistrationsAsync(createdEvent.Id, "?page=1"),
            await ListRegistrationsAsync(createdEvent.Id, "?page=2"),
            await ListRegistrationsAsync(createdEvent.Id, "?page=3"),
        };

        // Assert
        Assert.Equal([10, 10, 5], pages.Select(page => page.Items.Count));
        Assert.All(pages, page =>
        {
            Assert.Equal(25, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
        });
        Assert.Equal(emails, pages.SelectMany(page => page.Items).Select(r => r.Email));
    }

    [Fact]
    public async Task ListRegistrations_CustomPageSize_ReturnsRequestedNumberOfAttendees()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        for (var attendee = 1; attendee <= 7; attendee++)
        {
            await RegisterAsync(createdEvent.Id, $"attendee{attendee}@example.com");
        }

        // Act
        var page = await ListRegistrationsAsync(createdEvent.Id, "?page=2&pageSize=3");

        // Assert
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(2, page.Page);
        Assert.Equal(3, page.PageSize);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task ListRegistrations_PageBeyondEnd_ReturnsEmptyItemsWithTotals()
    {
        // Arrange
        var createdEvent = await CreateEventAsync();
        await RegisterAsync(createdEvent.Id, "ada@example.com");

        // Act
        var page = await ListRegistrationsAsync(createdEvent.Id, "?page=4");

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
    }

    [Theory]
    [InlineData("?page=0", "Page")]
    [InlineData("?pageSize=0", "PageSize")]
    [InlineData("?pageSize=101", "PageSize")]
    public async Task ListRegistrations_InvalidPaging_ReturnsValidationProblem(string query, string invalidField)
    {
        // Arrange
        var createdEvent = await CreateEventAsync();

        // Act
        var response = await Client.GetAsync($"/api/events/{createdEvent.Id}/registrations{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(invalidField, problem!.Errors.Keys);
    }

    [Fact]
    public async Task ListRegistrations_UnknownEvent_ReturnsNotFound()
    {
        // Arrange
        var unknownEventId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/events/{unknownEventId}/registrations");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("event_not_found", await response.ReadProblemCodeAsync());
    }
}
