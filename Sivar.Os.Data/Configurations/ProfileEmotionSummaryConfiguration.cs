using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity configuration for ProfileEmotionSummary
/// </summary>
public class ProfileEmotionSummaryConfiguration : IEntityTypeConfiguration<ProfileEmotionSummary>
{
    public void Configure(EntityTypeBuilder<ProfileEmotionSummary> builder)
    {
        builder.ToTable("Sivar_ProfileEmotionSummaries");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TimeWindow)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.DominantEmotion)
            .HasMaxLength(20);

        builder.Property(p => p.StartDate)
            .IsRequired();

        builder.Property(p => p.EndDate)
            .IsRequired();

        // Relationship with Profile
        builder.HasOne(p => p.Profile)
            .WithMany()
            .HasForeignKey(p => p.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint to prevent duplicates
        builder.HasIndex(p => new { p.ProfileId, p.TimeWindow, p.StartDate })
            .IsUnique()
            .HasDatabaseName("IX_ProfileEmotionSummaries_Unique");

        // Additional indexes for analytics queries
        builder.HasIndex(p => p.ProfileId)
            .HasDatabaseName("IX_ProfileEmotionSummaries_ProfileId");

        builder.HasIndex(p => p.TimeWindow)
            .HasDatabaseName("IX_ProfileEmotionSummaries_TimeWindow");

        builder.HasIndex(p => new { p.StartDate, p.EndDate })
            .HasDatabaseName("IX_ProfileEmotionSummaries_Dates");
    }
}
