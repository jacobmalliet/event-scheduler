using Microsoft.AspNetCore.Http.HttpResults;

namespace EventScheduler.Api.Common;

/// <summary>
/// Converts business-rule <see cref="Error"/>s into HTTP responses.
/// </summary>
public static class ErrorResults
{
    /// <summary>
    /// Maps a business-rule <see cref="Error"/> to an RFC 7807 problem response:
    /// <see cref="ErrorType.NotFound"/> → 404, <see cref="ErrorType.Conflict"/> → 409,
    /// with the error's <c>code</c> as an extension member.
    /// </summary>
    /// <param name="error">The business-rule failure.</param>
    /// <returns>A problem response with the status, the message as <c>title</c>, and the <c>code</c>.</returns>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return TypedResults.Problem(
            title: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
