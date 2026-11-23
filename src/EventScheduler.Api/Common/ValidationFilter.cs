using FluentValidation;

namespace EventScheduler.Api.Common;

/// <summary>
/// Runs the FluentValidation validator for <typeparamref name="TRequest"/> before the endpoint executes.
/// </summary>
/// <remarks>
/// The request is found among the endpoint's arguments by type, so this works the same for JSON bodies
/// and for query-string records bound with <c>[AsParameters]</c>.
/// </remarks>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <param name="validator">The validator for <typeparamref name="TRequest"/>, resolved from DI.</param>
public sealed class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
{
    /// <summary>
    /// Validates the request argument, then either short-circuits with a 400 problem or runs the endpoint.
    /// </summary>
    /// <param name="context">The endpoint invocation, including its arguments.</param>
    /// <param name="next">The rest of the pipeline, ending in the endpoint.</param>
    /// <returns>
    /// A 400 problem if the request is missing, a 400 validation problem if it's invalid, otherwise the endpoint's result.
    /// </returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return TypedResults.Problem("Request body is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}

/// <summary>
/// Attaches <see cref="ValidationFilter{TRequest}"/> to endpoints.
/// </summary>
public static class ValidationFilterExtensions
{
    /// <summary>
    /// Validates <typeparamref name="TRequest"/> before the endpoint runs, returning a 400 validation problem
    /// if it's invalid, and declares that 400 response in the OpenAPI document.
    /// </summary>
    /// <typeparam name="TRequest">The request type to validate.</typeparam>
    /// <param name="builder">The endpoint to validate.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>().ProducesValidationProblem();
}
