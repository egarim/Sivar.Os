using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// Entity configuration for AdTransaction
/// </summary>
public class AdTransactionConfiguration : IEntityTypeConfiguration<AdTransaction>
{
    public void Configure(EntityTypeBuilder<AdTransaction> builder)
    {
        builder.ToTable("AdTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 4);

        builder.Property(t => t.BalanceAfter)
            .HasPrecision(18, 4);

        builder.Property(t => t.Timestamp)
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .IsRequired();

        // Index on ProfileId for fast lookups
        builder.HasIndex(t => t.ProfileId);

        // Index on Timestamp for ordering
        builder.HasIndex(t => t.Timestamp);

        // Composite index for profile + time range queries
        builder.HasIndex(t => new { t.ProfileId, t.Timestamp });

        // Relationship to Profile
        builder.HasOne(t => t.Profile)
            .WithMany()
            .HasForeignKey(t => t.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
