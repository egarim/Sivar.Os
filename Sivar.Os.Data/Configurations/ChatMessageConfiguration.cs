using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;


namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ChatMessage entity
/// Note: This table is a TimescaleDB hypertable partitioned by CreatedAt.
/// Composite primary key (Id, CreatedAt) is REQUIRED for TimescaleDB unique constraints.
/// </summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        // Table name
        builder.ToTable("Sivar_ChatMessages");

        // Composite primary key - REQUIRED for TimescaleDB hypertable
        // TimescaleDB requires partitioning column (CreatedAt) in all unique constraints
        builder.HasKey(m => new { m.Id, m.CreatedAt });

        // Alternate key on Id - allows other entities to reference ChatMessage with single-column FK
        // Required because composite PK (Id, CreatedAt) can't be referenced by single Guid FK
        builder.HasAlternateKey(m => m.Id);

        // Properties
        builder.Property(m => m.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(m => m.StructuredResponse)
            .HasColumnType("text");

        builder.Property(m => m.MessageOrder)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .IsRequired();

        builder.Property(m => m.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => new { m.ConversationId, m.MessageOrder });

        // Global query filter for soft delete
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
