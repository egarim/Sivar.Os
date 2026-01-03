using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity Framework configuration for PhoneVerification entity
/// </summary>
public class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        // Table configuration
        builder.ToTable("Sivar_PhoneVerifications");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(p => p.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.TwilioVerificationSid)
            .HasMaxLength(50);

        builder.Property(p => p.RequestedAt)
            .IsRequired();

        builder.Property(p => p.ExpiresAt)
            .IsRequired();

        builder.Property(p => p.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(p => p.UserAgent)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_PhoneVerification_UserId");

        builder.HasIndex(p => p.PhoneNumber)
            .HasDatabaseName("IX_PhoneVerification_PhoneNumber");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_PhoneVerification_Status");

        builder.HasIndex(p => p.TwilioVerificationSid)
            .HasDatabaseName("IX_PhoneVerification_TwilioSid");

        builder.HasIndex(p => p.RequestedAt)
            .HasDatabaseName("IX_PhoneVerification_RequestedAt");

        // Composite index for rate limiting queries
        builder.HasIndex(p => new { p.PhoneNumber, p.RequestedAt })
            .HasDatabaseName("IX_PhoneVerification_Phone_RequestedAt");

        builder.HasIndex(p => new { p.IpAddress, p.RequestedAt })
            .HasDatabaseName("IX_PhoneVerification_IP_RequestedAt");
    }
}
