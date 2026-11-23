using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace EventScheduler.Api.Tests;

/// <summary>
/// Assertion helpers for API responses.
/// </summary>
internal static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Reads the stable business-rule <c>code</c> extension from a problem details response.
    /// </summary>
    /// <param name="response">A problem details response.</param>
    /// <returns>The <c>code</c>, or <see langword="null"/> if the response has none.</returns>
    public static async Task<string?> ReadProblemCodeAsync(this HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return problem?.Extensions.TryGetValue("code", out var code) == true ? code?.ToString() : null;
    }
}
