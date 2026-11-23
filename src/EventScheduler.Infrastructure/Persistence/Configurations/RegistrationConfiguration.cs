using EventScheduler.Application.Registrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventScheduler.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Registration"/>: column limits and the unique <c>(EventId, Email)</c> index.
/// </summary>
internal sealed class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(Registration.EmailMaxLength);

        // Not enforced by the InMemory provider (endpoints check explicitly),
        // but applies as soon as a relational provider is used.
        builder.HasIndex(r => new { r.EventId, r.Email }).IsUnique();
    }
}
