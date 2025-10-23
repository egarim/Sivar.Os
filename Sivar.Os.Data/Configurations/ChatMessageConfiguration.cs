using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;


namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ChatMessage entity
/// </summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        // Table name
        builder.ToTable("ChatMessages");

        // Primary key
        builder.HasKey(m => m.Id);

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
