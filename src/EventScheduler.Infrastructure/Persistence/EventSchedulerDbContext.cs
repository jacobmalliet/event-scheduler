using EventScheduler.Application.Events;
using EventScheduler.Application.Registrations;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Infrastructure.Persistence;

/// <summary>
/// EF Core context for events and registrations. Entity mappings live in <c>Configurations/</c>.
/// </summary>
/// <param name="options">The options configured by <see cref="DependencyInjection.AddInfrastructure"/>.</param>
public class EventSchedulerDbContext(DbContextOptions<EventSchedulerDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the events.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>
    /// Gets the registrations across all events.
    /// </summary>
    public DbSet<Registration> Registrations => Set<Registration>();

    /// <summary>
    /// Applies every <see cref="IEntityTypeConfiguration{TEntity}"/> in this assembly.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventSchedulerDbContext).Assembly);
}
