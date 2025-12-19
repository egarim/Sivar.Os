using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Defines recurrence rules for repeating events.
/// Based on RFC 5545 (iCalendar) RRULE specification.
/// </summary>
public class RecurrenceRule : BaseEntity
{
    /// <summary>
    /// Frequency of recurrence
    /// </summary>
    public virtual RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Weekly;

    /// <summary>
    /// Interval between occurrences (e.g., every 2 weeks)
    /// </summary>
    public virtual int Interval { get; set; } = 1;

    /// <summary>
    /// Days of the week for weekly recurrence (comma-separated: "MO,WE,FR")
    /// </summary>
    [MaxLength(50)]
    public virtual string? ByDay { get; set; }

    /// <summary>
    /// Days of the month for monthly recurrence (comma-separated: "1,15")
    /// </summary>
    [MaxLength(100)]
    public virtual string? ByMonthDay { get; set; }

    /// <summary>
    /// Months of the year for yearly recurrence (comma-separated: "1,6,12")
    /// </summary>
    [MaxLength(50)]
    public virtual string? ByMonth { get; set; }

    /// <summary>
    /// Week numbers for yearly recurrence (comma-separated: "1,52")
    /// </summary>
    [MaxLength(100)]
    public virtual string? ByWeekNo { get; set; }

    /// <summary>
    /// Position within the month (e.g., "2" for second occurrence, "-1" for last)
    /// </summary>
    public virtual int? BySetPos { get; set; }

    /// <summary>
    /// Start of the week (MO, TU, WE, TH, FR, SA, SU)
    /// </summary>
    [MaxLength(2)]
    public virtual string WeekStart { get; set; } = "SU";

    /// <summary>
    /// Maximum number of occurrences (null = no limit)
    /// </summary>
    public virtual int? Count { get; set; }

    /// <summary>
    /// End date for recurrence (null = no end date)
    /// </summary>
    public virtual DateTime? Until { get; set; }

    /// <summary>
    /// Exception dates (dates to skip, comma-separated ISO dates)
    /// </summary>
    [MaxLength(2000)]
    public virtual string? ExceptionDates { get; set; }

    /// <summary>
    /// The main event this rule belongs to
    /// </summary>
    public virtual Guid EventId { get; set; }

    /// <summary>
    /// Navigation property to the event
    /// </summary>
    public virtual ScheduleEvent? Event { get; set; }

    /// <summary>
    /// Get a human-readable description of the recurrence
    /// </summary>
    public string GetDescription()
    {
        var intervalText = Interval > 1 ? $"every {Interval} " : "every ";
        
        return Frequency switch
        {
            RecurrenceFrequency.Daily => $"{intervalText}day{(Interval > 1 ? "s" : "")}",
            RecurrenceFrequency.Weekly when !string.IsNullOrEmpty(ByDay) => 
                $"{intervalText}week on {ByDay}",
            RecurrenceFrequency.Weekly => $"{intervalText}week",
            RecurrenceFrequency.Monthly when !string.IsNullOrEmpty(ByMonthDay) => 
                $"{intervalText}month on day {ByMonthDay}",
            RecurrenceFrequency.Monthly => $"{intervalText}month",
            RecurrenceFrequency.Yearly => $"{intervalText}year",
            _ => "custom recurrence"
        };
    }

    /// <summary>
    /// Generate RRULE string for iCalendar compatibility
    /// </summary>
    public string ToRRule()
    {
        var parts = new List<string>
        {
            $"FREQ={Frequency.ToString().ToUpperInvariant()}",
            $"INTERVAL={Interval}"
        };

        if (!string.IsNullOrEmpty(ByDay))
            parts.Add($"BYDAY={ByDay}");
        
        if (!string.IsNullOrEmpty(ByMonthDay))
            parts.Add($"BYMONTHDAY={ByMonthDay}");
        
        if (!string.IsNullOrEmpty(ByMonth))
            parts.Add($"BYMONTH={ByMonth}");
        
        if (Count.HasValue)
            parts.Add($"COUNT={Count}");
        
        if (Until.HasValue)
            parts.Add($"UNTIL={Until.Value:yyyyMMddTHHmmssZ}");
        
        parts.Add($"WKST={WeekStart}");

        return string.Join(";", parts);
    }
}
