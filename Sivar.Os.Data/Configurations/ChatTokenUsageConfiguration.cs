using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ChatTokenUsage entity
/// </summary>
public class ChatTokenUsageConfiguration : IEntityTypeConfiguration<ChatTokenUsage>
{
    public void Configure(EntityTypeBuilder<ChatTokenUsage> builder)
    {
        // Table name
        builder.ToTable("Sivar_ChatTokenUsages");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(t => t.ProfileId)
            .IsRequired();

        builder.Property(t => t.ConversationId)
            .IsRequired(false);

        builder.Property(t => t.InputTokens)
            .IsRequired();

        builder.Property(t => t.OutputTokens)
            .IsRequired();

        builder.Property(t => t.TotalTokens)
            .IsRequired();

        builder.Property(t => t.ModelName)
            .HasMaxLength(100);

        builder.Property(t => t.Intent)
            .HasMaxLength(100);

        builder.Property(t => t.MessagePreview)
            .HasMaxLength(200);

        builder.Property(t => t.EstimatedCost)
            .HasPrecision(18, 8);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(t => t.Profile)
            .WithMany()
            .HasForeignKey(t => t.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common query patterns
        builder.HasIndex(t => t.ProfileId)
            .HasDatabaseName("IX_ChatTokenUsages_ProfileId");

        builder.HasIndex(t => t.ConversationId)
            .HasDatabaseName("IX_ChatTokenUsages_ConversationId");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_ChatTokenUsages_CreatedAt");

        // Composite index for efficient profile token queries by date range
        builder.HasIndex(t => new { t.ProfileId, t.CreatedAt })
            .HasDatabaseName("IX_ChatTokenUsages_ProfileId_CreatedAt");
    }
}
