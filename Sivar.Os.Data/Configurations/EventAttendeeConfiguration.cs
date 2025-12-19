using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for EventAttendee entity
/// </summary>
public class EventAttendeeConfiguration : IEntityTypeConfiguration<EventAttendee>
{
    public void Configure(EntityTypeBuilder<EventAttendee> builder)
    {
        builder.ToTable("Sivar_EventAttendees");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Note)
            .HasMaxLength(500);

        builder.Property(a => a.ConfirmationNumber)
            .HasMaxLength(50);

        builder.Property(a => a.PaymentTransactionId)
            .HasMaxLength(100);

        builder.Property(a => a.AmountPaid)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(a => a.Event)
            .WithMany(e => e.Attendees)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Profile)
            .WithMany()
            .HasForeignKey(a => a.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.EventId);
        builder.HasIndex(a => a.ProfileId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => new { a.EventId, a.ProfileId }).IsUnique();
        builder.HasIndex(a => new { a.EventId, a.Status });
        builder.HasIndex(a => a.IsDeleted);

        // Soft delete filter
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
