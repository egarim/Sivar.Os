using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Activity entity
/// </summary>
public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_Activities");

        // Primary key
        builder.HasKey(a => a.Id);

        // Indexes for common queries
        builder.HasIndex(a => a.ActorId)
            .HasDatabaseName("IX_Activities_ActorId");

        builder.HasIndex(a => new { a.ObjectType, a.ObjectId })
            .HasDatabaseName("IX_Activities_Object");

        builder.HasIndex(a => a.Verb)
            .HasDatabaseName("IX_Activities_Verb");

        builder.HasIndex(a => a.PublishedAt)
            .HasDatabaseName("IX_Activities_PublishedAt");

        builder.HasIndex(a => a.Visibility)
            .HasDatabaseName("IX_Activities_Visibility");

        builder.HasIndex(a => new { a.ActorId, a.PublishedAt })
            .HasDatabaseName("IX_Activities_ActorId_PublishedAt");

        builder.HasIndex(a => new { a.Visibility, a.IsPublished, a.PublishedAt })
            .HasDatabaseName("IX_Activities_Feed_Query");

        builder.HasIndex(a => a.EngagementScore)
            .HasDatabaseName("IX_Activities_EngagementScore");
        
        // GIN index for JSONB Metadata column (Phase 2: PostgreSQL optimization)
        builder.HasIndex(a => a.Metadata)
            .HasMethod("gin")
            .HasDatabaseName("IX_Activities_Metadata_Gin");

        // Relationships
        builder.HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Properties
        builder.Property(a => a.Verb)
            .IsRequired();

        builder.Property(a => a.ObjectType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.ObjectId)
            .IsRequired();

        builder.Property(a => a.TargetType)
            .HasMaxLength(50);

        builder.Property(a => a.Summary)
            .HasMaxLength(500);

        builder.Property(a => a.Metadata)
            .HasColumnType("jsonb") // PostgreSQL JSONB for efficient querying
            .HasDefaultValue("{}");

        builder.Property(a => a.Visibility)
            .IsRequired();

        builder.Property(a => a.IsPublished)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.PublishedAt)
            .IsRequired();

        builder.Property(a => a.Language)
            .IsRequired()
            .HasMaxLength(5)
            .HasDefaultValue("en");

        builder.Property(a => a.Priority)
            .HasDefaultValue(0);

        builder.Property(a => a.ViewCount)
            .HasDefaultValue(0);

        builder.Property(a => a.EngagementScore)
            .HasDefaultValue(0);

        // Soft delete configuration
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
