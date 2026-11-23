using EventScheduler.Application.Events;

namespace EventScheduler.Application.Registrations;

/// <summary>
/// One attendee's registration for an event. Attendees are identified by email.
/// </summary>
public class Registration
{
    /// <summary>
    /// The maximum length of <see cref="Email"/>.
    /// </summary>
    public const int EmailMaxLength = 320;

    /// <summary>
    /// Gets or sets the unique identifier of the registration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the event this registration is for.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the event this registration is for, when loaded.
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Gets or sets the attendee's email, stored normalized (trimmed, lower-case). See <see cref="NormalizeEmail"/>.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets when the registration was made, in UTC. Attendee lists are ordered by this.
    /// </summary>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    /// Normalizes an email for storage and comparison, so <c>" Ada@Example.com"</c> and
    /// <c>"ada@example.com"</c> count as the same attendee.
    /// </summary>
    /// <param name="email">The email as entered.</param>
    /// <returns>The email trimmed and in lower case.</returns>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
