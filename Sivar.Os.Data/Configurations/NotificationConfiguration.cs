using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Notification entity
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        // Table name
        builder.ToTable("Notifications");

        // Primary key
        builder.HasKey(n => n.Id);

        // Properties
        builder.Property(n => n.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Content)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.RelatedEntityId)
            .IsRequired(false);

        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(50);

        builder.Property(n => n.TriggeredByUserId)
            .IsRequired(false);

        builder.Property(n => n.Metadata)
            .HasColumnType("text");

        builder.Property(n => n.ReadAt)
            .IsRequired(false);

        builder.Property(n => n.Priority)
            .IsRequired()
            .HasDefaultValue(NotificationPriority.Normal);

        // Base entity properties
        builder.Property(n => n.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(n => n.UpdatedAt)
            .IsRequired();

        builder.Property(n => n.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.DeletedAt)
            .IsRequired(false);

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.TriggeredByUser)
            .WithMany()
            .HasForeignKey(n => n.TriggeredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for performance
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");

        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");

        builder.HasIndex(n => new { n.UserId, n.Type })
            .HasDatabaseName("IX_Notifications_UserId_Type");

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");

        builder.HasIndex(n => new { n.UserId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_CreatedAt");

        builder.HasIndex(n => new { n.RelatedEntityId, n.RelatedEntityType })
            .HasDatabaseName("IX_Notifications_RelatedEntity");

        builder.HasIndex(n => n.TriggeredByUserId)
            .HasDatabaseName("IX_Notifications_TriggeredByUserId");

        // Composite index for duplicate checking
        builder.HasIndex(n => new { n.UserId, n.Type, n.RelatedEntityId, n.TriggeredByUserId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_DuplicateCheck");

        // Soft delete filter
        builder.HasQueryFilter(n => !n.IsDeleted);

        // Check constraints for data integrity (PostgreSQL compatible syntax)
        builder.ToTable(t => t.HasCheckConstraint("CK_Notifications_ValidPriority", 
            "\"Priority\" >= 1 AND \"Priority\" <= 4"));

        builder.ToTable(t => t.HasCheckConstraint("CK_Notifications_ValidType",
            "\"Type\" IN ('Follow', 'Unfollow', 'Comment', 'Reply', 'Reaction', 'PostMention', 'CommentMention', 'System')"));
    }
}