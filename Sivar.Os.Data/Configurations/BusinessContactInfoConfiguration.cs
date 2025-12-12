using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for BusinessContactInfo entity
/// </summary>
public class BusinessContactInfoConfiguration : IEntityTypeConfiguration<BusinessContactInfo>
{
    public void Configure(EntityTypeBuilder<BusinessContactInfo> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_BusinessContactInfos");

        // Primary key
        builder.HasKey(bc => bc.Id);

        // Properties
        builder.Property(bc => bc.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(bc => bc.Label)
            .HasMaxLength(100);

        builder.Property(bc => bc.CountryCode)
            .HasMaxLength(10);

        builder.Property(bc => bc.SortOrder)
            .HasDefaultValue(100);

        builder.Property(bc => bc.IsPrimary)
            .HasDefaultValue(false);

        builder.Property(bc => bc.IsActive)
            .HasDefaultValue(true);

        // JSONB column for PostgreSQL
        builder.Property(bc => bc.AvailableHours)
            .HasColumnType("jsonb");

        builder.Property(bc => bc.Notes)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(bc => bc.ProfileId)
            .HasDatabaseName("IX_BusinessContactInfos_ProfileId");

        builder.HasIndex(bc => bc.ContactTypeId)
            .HasDatabaseName("IX_BusinessContactInfos_ContactTypeId");

        builder.HasIndex(bc => new { bc.ProfileId, bc.ContactTypeId })
            .HasDatabaseName("IX_BusinessContactInfos_Profile_ContactType");

        builder.HasIndex(bc => new { bc.ProfileId, bc.IsPrimary })
            .HasDatabaseName("IX_BusinessContactInfos_Profile_Primary");

        builder.HasIndex(bc => bc.IsActive)
            .HasDatabaseName("IX_BusinessContactInfos_IsActive");

        // Soft delete filter
        builder.HasQueryFilter(bc => !bc.IsDeleted);

        // Relationships
        builder.HasOne(bc => bc.Profile)
            .WithMany()
            .HasForeignKey(bc => bc.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bc => bc.ContactType)
            .WithMany(ct => ct.BusinessContacts)
            .HasForeignKey(bc => bc.ContactTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
