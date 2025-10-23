using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for SavedResult entity
/// </summary>
public class SavedResultConfiguration : IEntityTypeConfiguration<SavedResult>
{
    public void Configure(EntityTypeBuilder<SavedResult> builder)
    {
        // Table name
        builder.ToTable("SavedResults");

        // Primary key
        builder.HasKey(sr => sr.Id);

        // Properties
        builder.Property(sr => sr.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(sr => sr.ProfileId)
            .IsRequired();

        builder.Property(sr => sr.ConversationId)
            .IsRequired();

        builder.Property(sr => sr.ResultType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sr => sr.ResultData)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(sr => sr.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sr => sr.CreatedAt)
            .IsRequired();

        builder.Property(sr => sr.UpdatedAt)
            .IsRequired();

        builder.Property(sr => sr.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(sr => sr.Profile)
            .WithMany()
            .HasForeignKey(sr => sr.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sr => sr.Conversation)
            .WithMany(c => c.SavedResults)
            .HasForeignKey(sr => sr.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(sr => sr.ProfileId);
        builder.HasIndex(sr => sr.ConversationId);
        builder.HasIndex(sr => sr.ResultType);
        builder.HasIndex(sr => sr.CreatedAt);

        // Global query filter for soft delete
        builder.HasQueryFilter(sr => !sr.IsDeleted);
    }
}
