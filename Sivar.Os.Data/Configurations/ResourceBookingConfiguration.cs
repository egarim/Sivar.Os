using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for ResourceBooking entity
/// </summary>
public class ResourceBookingConfiguration : IEntityTypeConfiguration<ResourceBooking>
{
    public void Configure(EntityTypeBuilder<ResourceBooking> builder)
    {
        builder.ToTable("Sivar_ResourceBookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.StartTime)
            .IsRequired();

        builder.Property(b => b.EndTime)
            .IsRequired();

        builder.Property(b => b.TimeZone)
            .HasMaxLength(100)
            .HasDefaultValue("UTC");

        builder.Property(b => b.ConfirmationCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.CustomerNotes)
            .HasMaxLength(1000);

        builder.Property(b => b.InternalNotes)
            .HasMaxLength(1000);

        builder.Property(b => b.Price)
            .HasPrecision(18, 2);

        builder.Property(b => b.Currency)
            .HasMaxLength(3);

        builder.Property(b => b.PaymentTransactionId)
            .HasMaxLength(100);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.GuestCount)
            .HasDefaultValue(1);

        builder.Property(b => b.ReminderSent)
            .HasDefaultValue(false);

        builder.Property(b => b.IsPaid)
            .HasDefaultValue(false);

        builder.Property(b => b.Review)
            .HasMaxLength(2000);

        // Relationships
        builder.HasOne(b => b.Resource)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Service)
            .WithMany()
            .HasForeignKey(b => b.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.CustomerProfile)
            .WithMany()
            .HasForeignKey(b => b.CustomerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.OriginalBooking)
            .WithMany()
            .HasForeignKey(b => b.OriginalBookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.RescheduledToBooking)
            .WithMany()
            .HasForeignKey(b => b.RescheduledToBookingId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(b => b.ResourceId);
        builder.HasIndex(b => b.CustomerProfileId);
        builder.HasIndex(b => b.ServiceId);
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.ConfirmationCode).IsUnique();
        builder.HasIndex(b => b.StartTime);
        builder.HasIndex(b => b.EndTime);
        builder.HasIndex(b => new { b.ResourceId, b.StartTime, b.EndTime });
        builder.HasIndex(b => new { b.ResourceId, b.Status });
        builder.HasIndex(b => new { b.CustomerProfileId, b.Status });
        builder.HasIndex(b => new { b.ResourceId, b.StartTime, b.Status });
    }
}
