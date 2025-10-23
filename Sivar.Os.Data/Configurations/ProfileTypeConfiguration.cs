using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ProfileType entity
/// </summary>
public class ProfileTypeConfiguration : IEntityTypeConfiguration<ProfileType>
{
    public void Configure(EntityTypeBuilder<ProfileType> builder)
    {
        // Table configuration
        builder.ToTable("ProfileTypes");

        // Primary key
        builder.HasKey(pt => pt.Id);

        // Properties
        builder.Property(pt => pt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pt => pt.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(pt => pt.Description)
            .HasMaxLength(500);

        builder.Property(pt => pt.FeatureFlags)
            .HasColumnType("text")
            .HasDefaultValue("{}");

        builder.Property(pt => pt.SortOrder)
            .HasDefaultValue(0);

        // Indexes for performance
        builder.HasIndex(pt => pt.Name)
            .IsUnique()
            .HasDatabaseName("IX_ProfileTypes_Name");

        builder.HasIndex(pt => pt.IsActive)
            .HasDatabaseName("IX_ProfileTypes_IsActive");

        builder.HasIndex(pt => pt.SortOrder)
            .HasDatabaseName("IX_ProfileTypes_SortOrder");

        // Soft delete filter
        builder.HasQueryFilter(pt => !pt.IsDeleted);

        // Relationships
        builder.HasMany(pt => pt.Profiles)
            .WithOne(p => p.ProfileType)
            .HasForeignKey(p => p.ProfileTypeId)
            .OnDelete(DeleteBehavior.Restrict); // Don't allow deletion if profiles exist
    }
}