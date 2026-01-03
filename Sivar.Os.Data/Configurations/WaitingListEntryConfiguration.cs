using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for WaitingListEntry entity
/// </summary>
public class WaitingListEntryConfiguration : IEntityTypeConfiguration<WaitingListEntry>
{
    public void Configure(EntityTypeBuilder<WaitingListEntry> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_WaitingListEntries");

        // Primary key
        builder.HasKey(w => w.Id);

        // Properties
        builder.Property(w => w.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(w => w.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(w => w.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(w => w.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(w => w.Position)
            .IsRequired();

        builder.Property(w => w.JoinedAt)
            .IsRequired();

        builder.Property(w => w.ApprovedBy)
            .HasMaxLength(100);

        builder.Property(w => w.ReferralCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(w => w.UsedReferralCode)
            .HasMaxLength(10);

        builder.Property(w => w.VerificationChannel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(w => w.AdminNotes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.ReferredBy)
            .WithMany()
            .HasForeignKey(w => w.ReferredByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(w => w.UserId)
            .IsUnique()
            .HasDatabaseName("IX_WaitingList_UserId");

        builder.HasIndex(w => w.Email)
            .HasDatabaseName("IX_WaitingList_Email");

        builder.HasIndex(w => w.PhoneNumber)
            .HasDatabaseName("IX_WaitingList_PhoneNumber");

        builder.HasIndex(w => w.Status)
            .HasDatabaseName("IX_WaitingList_Status");

        builder.HasIndex(w => w.Position)
            .HasDatabaseName("IX_WaitingList_Position");

        builder.HasIndex(w => w.ReferralCode)
            .IsUnique()
            .HasDatabaseName("IX_WaitingList_ReferralCode");

        builder.HasIndex(w => w.UsedReferralCode)
            .HasDatabaseName("IX_WaitingList_UsedReferralCode");

        builder.HasIndex(w => w.CountryCode)
            .HasDatabaseName("IX_WaitingList_CountryCode");

        // Composite index for queue queries
        builder.HasIndex(w => new { w.Status, w.Position })
            .HasDatabaseName("IX_WaitingList_Status_Position");
    }
}
