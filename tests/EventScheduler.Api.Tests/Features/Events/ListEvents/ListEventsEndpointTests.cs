using System.Net;
using System.Net.Http.Json;
using EventScheduler.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace EventScheduler.Api.Tests.Features.Events.ListEvents;

public sealed class ListEventsEndpointTests : ApiTestBase
{
    [Fact]
    public async Task ListEvents_NoEvents_ReturnsEmptyFirstPage()
    {
        // Arrange
        const string NoQuery = "";

        // Act
        var page = await ListEventsAsync(NoQuery);

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(Paging.DefaultPageSize, page.PageSize);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task ListEvents_MultipleEvents_ReturnsSoonestFirstWithRegisteredCounts()
    {
        // Arrange
        var laterEvent = await CreateEventAsync(startsIn: TimeSpan.FromDays(10));
        var soonerEvent = await CreateEventAsync(startsIn: TimeSpan.FromDays(2));
        await RegisterAsync(laterEvent.Id, "ada@example.com");
        await RegisterAsync(laterEvent.Id, "grace@example.com");

        // Act
        var page = await ListEventsAsync();

        // Assert
        Assert.Collection(
            page.Items,
            first =>
            {
                Assert.Equal(soonerEvent.Id, first.Id);
                Assert.Equal(0, first.RegisteredCount);
            },
            second =>
            {
                Assert.Equal(laterEvent.Id, second.Id);
                Assert.Equal(2, second.RegisteredCount);
            });
    }

    [Fact]
    public async Task ListEvents_MoreEventsThanOnePage_ReturnsPagesOf10InOrderWithoutOverlap()
    {
        // Arrange
        var createdIds = new List<Guid>();
        for (var day = 1; day <= 25; day++)
        {
            createdIds.Add((await CreateEventAsync(startsIn: TimeSpan.FromDays(day))).Id);
        }

        // Act
        var pages = new[]
        {
            await ListEventsAsync("?page=1"),
            await ListEventsAsync("?page=2"),
            await ListEventsAsync("?page=3"),
        };

        // Assert
        Assert.Equal([10, 10, 5], pages.Select(page => page.Items.Count));
        Assert.All(pages, page =>
        {
            Assert.Equal(10, page.PageSize);
            Assert.Equal(25, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
        });
        Assert.Equal(createdIds, pages.SelectMany(page => page.Items).Select(e => e.Id));
    }

    [Fact]
    public async Task ListEvents_CustomPageSize_ReturnsRequestedNumberOfEvents()
    {
        // Arrange
        for (var day = 1; day <= 12; day++)
        {
            await CreateEventAsync(startsIn: TimeSpan.FromDays(day));
        }

        // Act
        var page = await ListEventsAsync("?page=2&pageSize=5");

        // Assert
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(2, page.Page);
        Assert.Equal(5, page.PageSize);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task ListEvents_PageBeyondEnd_ReturnsEmptyItemsWithTotals()
    {
        // Arrange
        await CreateEventAsync();

        // Act
        var page = await ListEventsAsync("?page=3");

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
    }

    [Theory]
    [InlineData("?page=0", "Page")]
    [InlineData("?page=2147483647&pageSize=100", "Page")]
    [InlineData("?pageSize=0", "PageSize")]
    [InlineData("?pageSize=101", "PageSize")]
    public async Task ListEvents_InvalidPaging_ReturnsValidationProblem(string query, string invalidField)
    {
        // Arrange
        var path = $"/api/events{query}";

        // Act
        var response = await Client.GetAsync(path);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(invalidField, problem!.Errors.Keys);
    }
}
