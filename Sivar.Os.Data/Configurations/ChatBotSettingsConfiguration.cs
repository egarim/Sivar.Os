using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ChatBotSettings entity
/// Phase 0.5: Configurable welcome messages and chat settings
/// </summary>
public class ChatBotSettingsConfiguration : IEntityTypeConfiguration<ChatBotSettings>
{
    public void Configure(EntityTypeBuilder<ChatBotSettings> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_ChatBotSettings");

        // Primary key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Culture)
            .HasMaxLength(10);

        builder.Property(s => s.WelcomeMessage)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(s => s.HeaderTagline)
            .HasMaxLength(100);

        builder.Property(s => s.BotName)
            .HasMaxLength(50)
            .HasDefaultValue("Sivar AI Assistant");

        builder.Property(s => s.QuickActionsJson)
            .HasColumnType("text");

        builder.Property(s => s.SystemPrompt)
            .HasMaxLength(5000);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.Priority)
            .HasDefaultValue(0);

        builder.Property(s => s.RegionCode)
            .HasMaxLength(10);

        builder.Property(s => s.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(s => s.ThinkingMessage)
            .HasMaxLength(200);

        // Base entity properties
        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(s => s.Key)
            .IsUnique()
            .HasDatabaseName("IX_ChatBotSettings_Key");

        builder.HasIndex(s => new { s.Culture, s.RegionCode, s.IsActive })
            .HasDatabaseName("IX_ChatBotSettings_Culture_Region_Active");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_ChatBotSettings_IsActive");

        // Query filter for soft delete
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
