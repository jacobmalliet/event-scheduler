using EventScheduler.Application.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventScheduler.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Event"/>: column limits and the cascade delete of its registrations.
/// </summary>
internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(Event.TitleMaxLength);
        builder.Property(e => e.Description).HasMaxLength(Event.DescriptionMaxLength);

        builder.HasMany(e => e.Registrations)
            .WithOne(r => r.Event)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
