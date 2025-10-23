using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for User entity
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.KeycloakId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(u => u.TimeZone)
            .HasMaxLength(50)
            .HasDefaultValue("UTC");

        // Indexes for performance
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.KeycloakId)
            .IsUnique()
            .HasDatabaseName("IX_Users_KeycloakId");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        // Soft delete filter
        builder.HasQueryFilter(u => !u.IsDeleted);

        // Relationships
        builder.HasMany(u => u.Profiles)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Active profile relationship (nullable foreign key)
        builder.HasOne(u => u.ActiveProfile)
            .WithMany()
            .HasForeignKey(u => u.ActiveProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        // Computed properties
        builder.Ignore(u => u.FullName);
        builder.Ignore(u => u.IsAdministrator);
    }
}