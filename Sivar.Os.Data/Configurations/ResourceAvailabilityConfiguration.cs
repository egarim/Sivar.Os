using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for ResourceAvailability entity
/// </summary>
public class ResourceAvailabilityConfiguration : IEntityTypeConfiguration<ResourceAvailability>
{
    public void Configure(EntityTypeBuilder<ResourceAvailability> builder)
    {
        builder.ToTable("Sivar_ResourceAvailability");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartTime)
            .IsRequired();

        builder.Property(a => a.EndTime)
            .IsRequired();

        builder.Property(a => a.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(a => a.Label)
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(a => a.Resource)
            .WithMany(r => r.Availability)
            .HasForeignKey(a => a.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.ResourceId);
        builder.HasIndex(a => new { a.ResourceId, a.DayOfWeek });
        builder.HasIndex(a => new { a.ResourceId, a.DayOfWeek, a.IsAvailable });
    }
}
