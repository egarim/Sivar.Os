using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;
using Sivar.Os.Shared.Enums;


namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Profile entity
/// </summary>
public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_Profiles");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Handle)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Bio)
            .HasMaxLength(2000);

        builder.Property(p => p.Avatar)
            .HasMaxLength(500);

        // JSON fields stored as text
        builder.Property(p => p.Metadata)
            .HasColumnType("text")
            .HasDefaultValue("{}");

        builder.Property(p => p.SocialMediaLinks)
            .HasColumnType("text") 
            .HasDefaultValue("{}");

        // List properties converted to JSON
        builder.Property(p => p.Tags)
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>());

        builder.Property(p => p.AllowedViewers)
            .HasColumnType("text")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<List<Guid>>(v, new JsonSerializerOptions()) ?? new List<Guid>());

        builder.Property(p => p.ViewCount)
            .HasDefaultValue(0);

        builder.Property(p => p.VisibilityLevel)
            .HasConversion<int>()
            .HasDefaultValue(VisibilityLevel.Public);

        builder.Property(p => p.Website)
            .HasMaxLength(500);

        builder.Property(p => p.ContactEmail)
            .HasMaxLength(256);

        builder.Property(p => p.ContactPhone)
            .HasMaxLength(20);

        builder.Property(p => p.ShowContactInfo)
            .HasDefaultValue(true);

        // Foreign keys
        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.ProfileTypeId)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Profiles_UserId");

        builder.HasIndex(p => p.ProfileTypeId)
            .HasDatabaseName("IX_Profiles_ProfileTypeId");

        builder.HasIndex(p => new { p.UserId, p.IsActive })
            .HasDatabaseName("IX_Profiles_UserId_IsActive");

        builder.HasIndex(p => p.VisibilityLevel)
            .HasDatabaseName("IX_Profiles_VisibilityLevel");

        builder.HasIndex(p => p.DisplayName)
            .HasDatabaseName("IX_Profiles_DisplayName");

        builder.HasIndex(p => p.Handle)
            .IsUnique()
            .HasDatabaseName("IX_Profiles_Handle");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Profiles_CreatedAt");

        // Composite index for user + profile type uniqueness (if needed later)
        builder.HasIndex(p => new { p.UserId, p.ProfileTypeId })
            .HasDatabaseName("IX_Profiles_UserId_ProfileTypeId");

        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany(u => u.Profiles)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ProfileType)
            .WithMany(pt => pt.Profiles)
            .HasForeignKey(p => p.ProfileTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Computed properties
        builder.Ignore(p => p.LocationDisplay);

        // ⚠️ CRITICAL: PostGIS GeoLocation column - IGNORED by EF Core
        // Reason: NetTopologySuite types incompatible with EF Core 9.0
        // This column is created via Database/Scripts/003_AddPostGISLocationSupport.sql
        builder.Ignore(p => p.GeoLocation);
        
        // GeoLocationUpdatedAt and GeoLocationSource are simple types - EF Core manages them
        builder.Property(p => p.GeoLocationUpdatedAt)
            .HasColumnName("GeoLocationUpdatedAt");
            
        builder.Property(p => p.GeoLocationSource)
            .HasMaxLength(20)
            .HasDefaultValue("Manual");
    }
}