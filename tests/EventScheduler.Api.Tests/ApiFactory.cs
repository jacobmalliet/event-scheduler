using System.Net.Http.Json;
using EventScheduler.Api.Features.Events.CreateEvent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace EventScheduler.Api.Tests;

/// <summary>
/// An API host with its own isolated in-memory database and a controllable clock.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Gets the clock the API reads "now" from. Starts at 2026-01-01 12:00 UTC; tests call
    /// <see cref="FakeTimeProvider.Advance"/> to move past an event's start time.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// Gives this host a uniquely named in-memory database and replaces the system clock with <see cref="Clock"/>.
    /// </summary>
    /// <param name="builder">The web host builder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Name", $"tests-{Guid.NewGuid()}");
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    /// <summary>
    /// Creates an event titled "Test event" through the API and fails the test if that doesn't succeed.
    /// </summary>
    /// <param name="client">The client to send the request with.</param>
    /// <param name="maxCapacity">The event's capacity.</param>
    /// <param name="startsIn">How long after <see cref="Clock"/>'s current time the event starts; defaults to 7 days.
    /// The event ends two hours after it starts.</param>
    /// <returns>The created event.</returns>
    public async Task<CreateEventResponse> CreateEventAsync(HttpClient client, int maxCapacity = 10, TimeSpan? startsIn = null)
    {
        var startsAt = Clock.GetUtcNow() + (startsIn ?? TimeSpan.FromDays(7));
        var request = new CreateEventRequest("Test event", "Description", startsAt, startsAt.AddHours(2), maxCapacity);

        var response = await client.PostAsJsonAsync("/api/events", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateEventResponse>())!;
    }
}
