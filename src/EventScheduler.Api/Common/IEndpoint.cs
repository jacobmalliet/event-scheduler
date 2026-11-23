using System.Reflection;

namespace EventScheduler.Api.Common;

/// <summary>
/// Implemented by each feature slice to map its own route(s).
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps this feature's route(s) and their metadata (validation, OpenAPI responses, name, tags).
    /// </summary>
    /// <param name="app">The route builder to map onto.</param>
    void MapEndpoint(IEndpointRouteBuilder app);
}

/// <summary>
/// Discovers and maps every <see cref="IEndpoint"/> in an assembly, so adding a feature never requires
/// editing <c>Program.cs</c>.
/// </summary>
public static class SliceRegistration
{
    /// <summary>
    /// Registers every non-abstract <see cref="IEndpoint"/> implementation in <paramref name="assembly"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="assembly">The assembly to scan for endpoints.</param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddSlices(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var endpointType in endpointTypes)
        {
            services.AddSingleton(typeof(IEndpoint), endpointType);
        }

        return services;
    }

    /// <summary>
    /// Maps the routes of every endpoint registered by <see cref="AddSlices"/>.
    /// </summary>
    /// <param name="app">The route builder to map onto.</param>
    /// <returns>The same <paramref name="app"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapSlices(this IEndpointRouteBuilder app)
    {
        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
