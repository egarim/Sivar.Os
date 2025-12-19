using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Represents a reminder for a scheduled event.
/// Multiple reminders can be set for a single event.
/// </summary>
public class EventReminder : BaseEntity
{
    /// <summary>
    /// The event this reminder is for
    /// </summary>
    public virtual Guid EventId { get; set; }

    /// <summary>
    /// Navigation property to the event
    /// </summary>
    public virtual ScheduleEvent? Event { get; set; }

    /// <summary>
    /// The profile to remind (usually the event owner or attendee)
    /// </summary>
    public virtual Guid ProfileId { get; set; }

    /// <summary>
    /// Navigation property to the profile
    /// </summary>
    public virtual Profile? Profile { get; set; }

    /// <summary>
    /// When to send the reminder (calculated from event start time)
    /// </summary>
    public virtual DateTime ReminderTime { get; set; }

    /// <summary>
    /// Minutes before event to send reminder (e.g., 15, 30, 60, 1440 for 1 day)
    /// </summary>
    public virtual int MinutesBefore { get; set; } = 30;

    /// <summary>
    /// Type of reminder notification
    /// </summary>
    public virtual ReminderType ReminderType { get; set; } = ReminderType.Push;

    /// <summary>
    /// Whether the reminder has been sent
    /// </summary>
    public virtual bool IsSent { get; set; }

    /// <summary>
    /// When the reminder was sent
    /// </summary>
    public virtual DateTime? SentAt { get; set; }

    /// <summary>
    /// Custom message for the reminder (optional)
    /// </summary>
    [MaxLength(500)]
    public virtual string? CustomMessage { get; set; }
}
