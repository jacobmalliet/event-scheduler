namespace EventScheduler.Api.Common;

/// <summary>
/// Kind of business-rule failure. Decides the HTTP status: see <see cref="ErrorResults.ToProblem"/>.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// The entity the request refers to doesn't exist. Maps to 404 Not Found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The request conflicts with the current state, e.g. the event is full. Maps to 409 Conflict.
    /// </summary>
    Conflict
}

/// <summary>
/// A business-rule failure returned by an endpoint (validation failures are handled by <see cref="ValidationFilter{TRequest}"/>).
/// </summary>
/// <param name="Type">Decides the HTTP status code.</param>
/// <param name="Code">Stable snake_case identifier clients and tests rely on, e.g. <c>event_full</c>. Don't rename.</param>
/// <param name="Message">Human-readable explanation, returned as the problem <c>title</c>.</param>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    /// <summary>
    /// Creates a <see cref="ErrorType.NotFound"/> error.
    /// </summary>
    /// <param name="code">The stable snake_case error code, e.g. <c>event_not_found</c>.</param>
    /// <param name="message">The human-readable explanation.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    /// <summary>
    /// Creates a <see cref="ErrorType.Conflict"/> error.
    /// </summary>
    /// <param name="code">The stable snake_case error code, e.g. <c>event_full</c>.</param>
    /// <param name="message">The human-readable explanation.</param>
    /// <returns>The error.</returns>
    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
}
