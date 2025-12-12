using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for QuickAction entity
/// Quick action buttons shown in chat interface
/// </summary>
public class QuickActionConfiguration : IEntityTypeConfiguration<QuickAction>
{
    public void Configure(EntityTypeBuilder<QuickAction> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_QuickActions");

        // Primary key
        builder.HasKey(q => q.Id);

        // Properties
        builder.Property(q => q.ChatBotSettingsId)
            .IsRequired();

        builder.Property(q => q.CapabilityId);

        builder.Property(q => q.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(q => q.Icon)
            .HasMaxLength(50);

        builder.Property(q => q.MudBlazorIcon)
            .HasMaxLength(100);

        builder.Property(q => q.Color)
            .HasMaxLength(20);

        builder.Property(q => q.DefaultQuery)
            .HasMaxLength(500);

        builder.Property(q => q.ContextHint)
            .HasMaxLength(500);

        builder.Property(q => q.SortOrder)
            .HasDefaultValue(0);

        builder.Property(q => q.IsActive)
            .HasDefaultValue(true);

        builder.Property(q => q.RequiresLocation)
            .HasDefaultValue(false);

        // Base entity properties
        builder.Property(q => q.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(q => q.IsDeleted)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(q => q.Capability)
            .WithMany()
            .HasForeignKey(q => q.CapabilityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(q => q.ChatBotSettingsId)
            .HasDatabaseName("IX_QuickActions_ChatBotSettingsId");

        builder.HasIndex(q => q.CapabilityId)
            .HasDatabaseName("IX_QuickActions_CapabilityId");

        builder.HasIndex(q => new { q.ChatBotSettingsId, q.SortOrder })
            .HasDatabaseName("IX_QuickActions_Settings_SortOrder");

        builder.HasIndex(q => q.IsActive)
            .HasDatabaseName("IX_QuickActions_IsActive");

        // Query filter for soft delete
        builder.HasQueryFilter(q => !q.IsDeleted);
    }
}
