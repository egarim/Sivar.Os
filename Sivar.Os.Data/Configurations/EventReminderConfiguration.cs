using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Data.Configurations;

/// <summary>
/// EF Core configuration for EventReminder entity
/// </summary>
public class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
    public void Configure(EntityTypeBuilder<EventReminder> builder)
    {
        builder.ToTable("Sivar_EventReminders");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.CustomMessage)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(r => r.Event)
            .WithMany(e => e.Reminders)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Profile)
            .WithMany()
            .HasForeignKey(r => r.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(r => r.EventId);
        builder.HasIndex(r => r.ProfileId);
        builder.HasIndex(r => r.ReminderTime);
        builder.HasIndex(r => r.IsSent);
        builder.HasIndex(r => new { r.IsSent, r.ReminderTime });
        builder.HasIndex(r => r.IsDeleted);

        // Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
