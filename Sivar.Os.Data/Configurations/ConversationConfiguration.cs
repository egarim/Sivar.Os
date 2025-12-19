using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Conversation entity
/// </summary>
public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        // Table name
        builder.ToTable("Sivar_Conversations");

        // Primary key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(c => c.ProfileId)
            .IsRequired();

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.LastMessageAt)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Token usage and cost totals (denormalized for performance)
        builder.Property(c => c.TotalInputTokens)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.TotalOutputTokens)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.TotalTokens)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.TotalCost)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasPrecision(18, 6);

        // Relationships
        builder.HasOne(c => c.Profile)
            .WithMany()
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.SavedResults)
            .WithOne(sr => sr.Conversation)
            .HasForeignKey(sr => sr.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.ProfileId);
        builder.HasIndex(c => c.LastMessageAt);
        builder.HasIndex(c => new { c.ProfileId, c.IsActive });

        // Global query filter for soft delete
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
