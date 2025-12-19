using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for RecurrenceRule entity
/// </summary>
public class RecurrenceRuleConfiguration : IEntityTypeConfiguration<RecurrenceRule>
{
    public void Configure(EntityTypeBuilder<RecurrenceRule> builder)
    {
        builder.ToTable("Sivar_RecurrenceRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ByDay)
            .HasMaxLength(50);

        builder.Property(r => r.ByMonthDay)
            .HasMaxLength(100);

        builder.Property(r => r.ByMonth)
            .HasMaxLength(50);

        builder.Property(r => r.ByWeekNo)
            .HasMaxLength(100);

        builder.Property(r => r.WeekStart)
            .HasMaxLength(2)
            .HasDefaultValue("SU");

        builder.Property(r => r.ExceptionDates)
            .HasMaxLength(2000);

        // Relationship is configured in ScheduleEventConfiguration
        // This just sets up the navigation property
        builder.HasOne(r => r.Event)
            .WithOne(e => e.RecurrenceRule)
            .HasForeignKey<RecurrenceRule>(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.EventId).IsUnique();
        builder.HasIndex(r => r.Frequency);
        builder.HasIndex(r => r.IsDeleted);

        // Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
