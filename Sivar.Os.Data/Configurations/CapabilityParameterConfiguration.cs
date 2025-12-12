using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for CapabilityParameter entity
/// Defines parameters for AI agent capabilities
/// </summary>
public class CapabilityParameterConfiguration : IEntityTypeConfiguration<CapabilityParameter>
{
    public void Configure(EntityTypeBuilder<CapabilityParameter> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_CapabilityParameters");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.CapabilityId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.DisplayName)
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.DataType)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("string");

        builder.Property(p => p.IsRequired)
            .HasDefaultValue(false);

        builder.Property(p => p.DefaultValue)
            .HasMaxLength(500);

        builder.Property(p => p.AllowedValuesJson)
            .HasColumnType("text");

        builder.Property(p => p.SortOrder)
            .HasDefaultValue(0);

        // Base entity properties
        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => p.CapabilityId)
            .HasDatabaseName("IX_CapabilityParameters_CapabilityId");

        builder.HasIndex(p => new { p.CapabilityId, p.Name })
            .IsUnique()
            .HasDatabaseName("IX_CapabilityParameters_Capability_Name");

        // Query filter for soft delete
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
