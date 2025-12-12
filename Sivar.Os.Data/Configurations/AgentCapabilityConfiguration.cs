using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AgentCapability entity
/// Defines AI agent capabilities/functions that can be invoked
/// </summary>
public class AgentCapabilityConfiguration : IEntityTypeConfiguration<AgentCapability>
{
    public void Configure(EntityTypeBuilder<AgentCapability> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_AgentCapabilities");

        // Primary key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Key)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.FunctionName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Category)
            .HasMaxLength(50);

        builder.Property(c => c.ExampleQueriesJson)
            .HasColumnType("text");

        builder.Property(c => c.UsageInstructions)
            .HasMaxLength(2000);

        builder.Property(c => c.IsEnabled)
            .HasDefaultValue(true);

        builder.Property(c => c.SortOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.Icon)
            .HasMaxLength(50);

        // Base entity properties
        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.IsDeleted)
            .HasDefaultValue(false);

        // Relationships
        builder.HasMany(c => c.Parameters)
            .WithOne(p => p.Capability)
            .HasForeignKey(p => p.CapabilityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.Key)
            .IsUnique()
            .HasDatabaseName("IX_AgentCapabilities_Key");

        builder.HasIndex(c => c.FunctionName)
            .HasDatabaseName("IX_AgentCapabilities_FunctionName");

        builder.HasIndex(c => c.Category)
            .HasDatabaseName("IX_AgentCapabilities_Category");

        builder.HasIndex(c => c.IsEnabled)
            .HasDatabaseName("IX_AgentCapabilities_IsEnabled");

        // Query filter for soft delete
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
