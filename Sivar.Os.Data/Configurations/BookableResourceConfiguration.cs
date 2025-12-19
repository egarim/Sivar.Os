using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for BookableResource entity
/// </summary>
public class BookableResourceConfiguration : IEntityTypeConfiguration<BookableResource>
{
    public void Configure(EntityTypeBuilder<BookableResource> builder)
    {
        builder.ToTable("Sivar_BookableResources");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.ImageUrl)
            .HasMaxLength(500);

        builder.Property(r => r.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(r => r.DefaultPrice)
            .HasPrecision(18, 2);

        builder.Property(r => r.SlotDurationMinutes)
            .HasDefaultValue(30);

        builder.Property(r => r.BufferMinutes)
            .HasDefaultValue(0);

        builder.Property(r => r.MaxConcurrentBookings)
            .HasDefaultValue(1);

        builder.Property(r => r.MinAdvanceBookingHours)
            .HasDefaultValue(1);

        builder.Property(r => r.MaxAdvanceBookingDays)
            .HasDefaultValue(30);

        builder.Property(r => r.CancellationWindowHours)
            .HasDefaultValue(24);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.Property(r => r.IsVisible)
            .HasDefaultValue(true);

        builder.Property(r => r.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(r => r.Tags)
            .HasColumnType("text[]");

        // Relationships
        builder.HasOne(r => r.Profile)
            .WithMany()
            .HasForeignKey(r => r.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: Post has composite PK (Id, CreatedAt) for TimescaleDB hypertable
        // Use HasPrincipalKey to reference the alternate key (Id) instead of composite PK
        builder.HasOne(r => r.Post)
            .WithMany()
            .HasForeignKey(r => r.PostId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(r => r.ProfileId);
        builder.HasIndex(r => r.PostId);
        builder.HasIndex(r => r.ResourceType);
        builder.HasIndex(r => r.Category);
        builder.HasIndex(r => r.IsActive);
        builder.HasIndex(r => r.IsVisible);
        builder.HasIndex(r => new { r.ProfileId, r.IsActive, r.IsVisible });
        builder.HasIndex(r => r.Tags).HasMethod("GIN");
    }
}
