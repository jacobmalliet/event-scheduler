using System.Net.Http.Json;
using EventScheduler.Api.Common;
using EventScheduler.Api.Features.Events.CreateEvent;
using EventScheduler.Api.Features.Events.ListEvents;
using EventScheduler.Api.Features.Registrations.ListRegistrations;
using EventScheduler.Api.Features.Registrations.RegisterForEvent;

namespace EventScheduler.Api.Tests;

/// <summary>
/// Base class for endpoint tests: xUnit creates a new instance per test, so every test gets its own
/// API host, isolated in-memory database and fake clock.
/// </summary>
public abstract class ApiTestBase : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiTestBase"/> class, starting a fresh API host.
    /// </summary>
    protected ApiTestBase() => Client = Factory.CreateClient();

    /// <summary>
    /// Gets the API host for this test, including its fake clock (<see cref="ApiFactory.Clock"/>).
    /// </summary>
    protected ApiFactory Factory { get; } = new();

    /// <summary>
    /// Gets the HTTP client that sends requests to <see cref="Factory"/>.
    /// </summary>
    protected HttpClient Client { get; }

    /// <summary>
    /// Disposes the client and shuts down the API host.
    /// </summary>
    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates an event through the API; see <see cref="ApiFactory.CreateEventAsync"/>.
    /// </summary>
    /// <param name="maxCapacity">The event's capacity.</param>
    /// <param name="startsIn">How long from now the event starts; defaults to 7 days.</param>
    /// <returns>The created event.</returns>
    protected Task<CreateEventResponse> CreateEventAsync(int maxCapacity = 10, TimeSpan? startsIn = null) =>
        Factory.CreateEventAsync(Client, maxCapacity, startsIn);

    /// <summary>
    /// Sends <c>POST /api/events/{eventId}/registrations</c>.
    /// </summary>
    /// <param name="eventId">The event to register for.</param>
    /// <param name="email">The attendee's email, sent as entered.</param>
    /// <returns>The raw response, so tests can assert on failures as well as successes.</returns>
    protected Task<HttpResponseMessage> RegisterAsync(Guid eventId, string email) =>
        Client.PostAsJsonAsync($"/api/events/{eventId}/registrations", new RegisterForEventRequest(email));

    /// <summary>
    /// Registers an email for an event and fails the test if that doesn't succeed.
    /// </summary>
    /// <param name="eventId">The event to register for.</param>
    /// <param name="email">The attendee's email, sent as entered.</param>
    /// <returns>The created registration, including its id for unregistering.</returns>
    protected async Task<RegisterForEventResponse> RegisterAttendeeAsync(Guid eventId, string email)
    {
        var response = await RegisterAsync(eventId, email);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterForEventResponse>())!;
    }

    /// <summary>
    /// Sends <c>DELETE /api/events/{eventId}/registrations/{registrationId}</c>.
    /// </summary>
    /// <param name="eventId">The event to unregister from.</param>
    /// <param name="registrationId">The registration to remove.</param>
    /// <returns>The raw response, so tests can assert on failures as well as successes.</returns>
    protected Task<HttpResponseMessage> UnregisterAsync(Guid eventId, Guid registrationId) =>
        Client.DeleteAsync($"/api/events/{eventId}/registrations/{registrationId}");

    /// <summary>
    /// Reads a page of events and fails the test if the request doesn't succeed.
    /// </summary>
    /// <param name="query">The query string including its leading <c>?</c>, e.g. <c>"?page=2"</c>; empty for the defaults.</param>
    /// <returns>The page of events.</returns>
    protected async Task<PagedResponse<ListEventsResponse>> ListEventsAsync(string query = "") =>
        (await Client.GetFromJsonAsync<PagedResponse<ListEventsResponse>>($"/api/events{query}"))!;

    /// <summary>
    /// Reads a page of an event's registrations and fails the test if the request doesn't succeed.
    /// </summary>
    /// <param name="eventId">The event whose registrations to read.</param>
    /// <param name="query">The query string including its leading <c>?</c>, e.g. <c>"?page=2"</c>; empty for the defaults.</param>
    /// <returns>The page of registrations, in the order they were made.</returns>
    protected async Task<PagedResponse<ListRegistrationsResponse>> ListRegistrationsAsync(Guid eventId, string query = "") =>
        (await Client.GetFromJsonAsync<PagedResponse<ListRegistrationsResponse>>($"/api/events/{eventId}/registrations{query}"))!;
}
