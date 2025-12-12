using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ContactType entity
/// </summary>
public class ContactTypeConfiguration : IEntityTypeConfiguration<ContactType>
{
    public void Configure(EntityTypeBuilder<ContactType> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_ContactTypes");

        // Primary key
        builder.HasKey(ct => ct.Id);

        // Properties
        builder.Property(ct => ct.Key)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ct => ct.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ct => ct.Icon)
            .HasMaxLength(50);

        builder.Property(ct => ct.MudBlazorIcon)
            .HasMaxLength(100);

        builder.Property(ct => ct.Color)
            .HasMaxLength(20);

        builder.Property(ct => ct.UrlTemplate)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ct => ct.Category)
            .HasMaxLength(30)
            .HasDefaultValue("other");

        builder.Property(ct => ct.SortOrder)
            .HasDefaultValue(100);

        builder.Property(ct => ct.IsActive)
            .HasDefaultValue(true);

        // JSONB columns for PostgreSQL
        builder.Property(ct => ct.RegionalPopularity)
            .HasColumnType("jsonb");

        builder.Property(ct => ct.Metadata)
            .HasColumnType("jsonb");

        builder.Property(ct => ct.ValidationRegex)
            .HasMaxLength(200);

        builder.Property(ct => ct.Placeholder)
            .HasMaxLength(100);

        builder.Property(ct => ct.OpenInNewTab)
            .HasDefaultValue(true);

        builder.Property(ct => ct.MobileOnly)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(ct => ct.Key)
            .IsUnique()
            .HasDatabaseName("IX_ContactTypes_Key");

        builder.HasIndex(ct => ct.Category)
            .HasDatabaseName("IX_ContactTypes_Category");

        builder.HasIndex(ct => ct.IsActive)
            .HasDatabaseName("IX_ContactTypes_IsActive");

        builder.HasIndex(ct => new { ct.Category, ct.SortOrder })
            .HasDatabaseName("IX_ContactTypes_Category_SortOrder");

        // Soft delete filter
        builder.HasQueryFilter(ct => !ct.IsDeleted);

        // Relationships
        builder.HasMany(ct => ct.BusinessContacts)
            .WithOne(bc => bc.ContactType)
            .HasForeignKey(bc => bc.ContactTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
