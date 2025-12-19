using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for ResourceService entity
/// </summary>
public class ResourceServiceConfiguration : IEntityTypeConfiguration<ResourceService>
{
    public void Configure(EntityTypeBuilder<ResourceService> builder)
    {
        builder.ToTable("Sivar_ResourceServices");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Price)
            .HasPrecision(18, 2);

        builder.Property(s => s.Currency)
            .HasMaxLength(3);

        builder.Property(s => s.ImageUrl)
            .HasMaxLength(500);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.DisplayOrder)
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(s => s.Resource)
            .WithMany(r => r.Services)
            .HasForeignKey(s => s.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(s => s.ResourceId);
        builder.HasIndex(s => new { s.ResourceId, s.IsActive });
    }
}
