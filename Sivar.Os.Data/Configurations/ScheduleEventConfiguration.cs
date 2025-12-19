using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for ScheduleEvent entity
/// </summary>
public class ScheduleEventConfiguration : IEntityTypeConfiguration<ScheduleEvent>
{
    public void Configure(EntityTypeBuilder<ScheduleEvent> builder)
    {
        builder.ToTable("Sivar_ScheduleEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(5000);

        builder.Property(e => e.TimeZone)
            .HasMaxLength(100)
            .HasDefaultValue("America/El_Salvador");

        builder.Property(e => e.Location)
            .HasMaxLength(500);

        builder.Property(e => e.VirtualLink)
            .HasMaxLength(500);

        builder.Property(e => e.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Color)
            .HasMaxLength(7);

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(e => e.Category)
            .HasMaxLength(100);

        builder.Property(e => e.ExternalCalendarId)
            .HasMaxLength(500);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(e => e.Profile)
            .WithMany()
            .HasForeignKey(e => e.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.RecurrenceRule)
            .WithOne(r => r.Event)
            .HasForeignKey<ScheduleEvent>(e => e.RecurrenceRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ParentEvent)
            .WithMany(e => e.ChildEvents)
            .HasForeignKey(e => e.ParentEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attendees)
            .WithOne(a => a.Event)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Reminders)
            .WithOne(r => r.Event)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.ProfileId);
        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => e.EndTime);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Visibility);
        builder.HasIndex(e => e.IsDeleted);
        builder.HasIndex(e => new { e.StartTime, e.EndTime });
        builder.HasIndex(e => new { e.ProfileId, e.StartTime });
        builder.HasIndex(e => new { e.Visibility, e.StartTime, e.Status });

        // Soft delete filter
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
