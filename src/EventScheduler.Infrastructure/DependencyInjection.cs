using EventScheduler.Infrastructure.Concurrency;
using EventScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventScheduler.Infrastructure;

/// <summary>
/// Registers the infrastructure services: persistence and concurrency.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the EF Core context (in-memory store) and the per-event <see cref="KeyedLock{TKey}"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="databaseName">
    /// Name of the in-memory store. Contexts with the same name share data, so tests pass a unique name per test.
    /// </param>
    /// <returns>The same <paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databaseName = "EventScheduler")
    {
        // Can be easily swapped for UseSqlServer to move to a SQL backend.
        services.AddDbContext<EventSchedulerDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton(typeof(KeyedLock<>));
        return services;
    }
}
