using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AiModelPricing entity
/// </summary>
public class AiModelPricingConfiguration : IEntityTypeConfiguration<AiModelPricing>
{
    public void Configure(EntityTypeBuilder<AiModelPricing> builder)
    {
        // Table name
        builder.ToTable("Sivar_AiModelPricings");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(p => p.ModelId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.ModelType)
            .IsRequired();

        builder.Property(p => p.Tier)
            .IsRequired();

        // Pricing with high precision for small values (8 decimal places)
        builder.Property(p => p.InputCostPer1M)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(p => p.OutputCostPer1M)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(p => p.BatchInputCostPer1M)
            .HasPrecision(18, 6);

        builder.Property(p => p.BatchOutputCostPer1M)
            .HasPrecision(18, 6);

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(p => p.ModelId)
            .IsUnique()
            .HasDatabaseName("IX_AiModelPricing_ModelId");

        builder.HasIndex(p => p.Provider)
            .HasDatabaseName("IX_AiModelPricing_Provider");

        builder.HasIndex(p => p.ModelType)
            .HasDatabaseName("IX_AiModelPricing_ModelType");

        builder.HasIndex(p => p.Tier)
            .HasDatabaseName("IX_AiModelPricing_Tier");

        builder.HasIndex(p => new { p.ModelType, p.IsDefault })
            .HasDatabaseName("IX_AiModelPricing_ModelType_IsDefault");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_AiModelPricing_IsActive");

        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
