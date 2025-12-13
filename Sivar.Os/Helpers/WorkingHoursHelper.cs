using System.Text.Json;
using Sivar.Os.Shared.Entities;

namespace Sivar.Os.Helpers;

/// <summary>
/// Helper class to parse working hours and calculate open/closed status.
/// El Salvador timezone is UTC-6 with no daylight saving time.
/// </summary>
public static class WorkingHoursHelper
{
    // El Salvador is UTC-6 with no daylight saving
    private static readonly TimeSpan ElSalvadorOffset = TimeSpan.FromHours(-6);

    /// <summary>
    /// Result of parsing working hours and calculating status
    /// </summary>
    public record OpenStatusResult(
        bool? IsOpenNow,
        string? ClosingTime,
        string? NextOpenTime,
        string? OpenStatusText
    );

    /// <summary>
    /// Calculates the open status for a business based on its working hours.
    /// </summary>
    /// <param name="workingHoursJson">JSON string containing BusinessHours data</param>
    /// <param name="utcNow">Optional UTC time for testing, defaults to DateTime.UtcNow</param>
    /// <returns>OpenStatusResult with all status fields populated</returns>
    public static OpenStatusResult CalculateOpenStatus(string? workingHoursJson, DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(workingHoursJson))
        {
            return new OpenStatusResult(null, null, null, null);
        }

        try
        {
            var businessHours = ParseWorkingHours(workingHoursJson);
            if (businessHours == null)
            {
                return new OpenStatusResult(null, null, null, null);
            }

            return CalculateOpenStatusFromHours(businessHours, utcNow ?? DateTime.UtcNow);
        }
        catch
        {
            return new OpenStatusResult(null, null, null, null);
        }
    }

    /// <summary>
    /// Parses JSON working hours into BusinessHours object.
    /// Handles both the structured object format and the nested businessMetadata format.
    /// </summary>
    private static BusinessHours? ParseWorkingHours(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Try parsing as direct BusinessHours object
        try
        {
            return JsonSerializer.Deserialize<BusinessHours>(json, options);
        }
        catch
        {
            // Not a direct BusinessHours object, try other formats
        }

        // Try parsing from a wrapper that contains workingHours property
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("workingHours", out var workingHoursElement) ||
                root.TryGetProperty("WorkingHours", out workingHoursElement))
            {
                return JsonSerializer.Deserialize<BusinessHours>(workingHoursElement.GetRawText(), options);
            }
        }
        catch
        {
            // Continue to next format
        }

        return null;
    }

    /// <summary>
    /// Calculates open status from parsed BusinessHours
    /// </summary>
    private static OpenStatusResult CalculateOpenStatusFromHours(BusinessHours hours, DateTime utcNow)
    {
        // Convert UTC to El Salvador time
        var elSalvadorTime = utcNow.Add(ElSalvadorOffset);
        var currentTime = TimeOnly.FromDateTime(elSalvadorTime);
        var currentDay = elSalvadorTime.DayOfWeek;

        var todaySchedule = GetScheduleForDay(hours, currentDay);

        // Check if closed today
        if (todaySchedule.IsClosed || todaySchedule.OpenTime == null || todaySchedule.CloseTime == null)
        {
            var (nextOpenDay, nextOpenSchedule) = FindNextOpenDay(hours, currentDay);
            var nextOpenTime = FormatNextOpenTime(nextOpenDay, nextOpenSchedule, currentDay);
            
            return new OpenStatusResult(
                IsOpenNow: false,
                ClosingTime: null,
                NextOpenTime: nextOpenTime,
                OpenStatusText: nextOpenTime != null ? $"Cerrado · Abre {nextOpenTime}" : "Cerrado"
            );
        }

        var openTime = todaySchedule.OpenTime.Value;
        var closeTime = todaySchedule.CloseTime.Value;

        // Check if currently within business hours
        bool isOpen = IsTimeWithinRange(currentTime, openTime, closeTime, todaySchedule);

        if (isOpen)
        {
            // Check if closing soon (within 1 hour)
            var closingTimeFormatted = closeTime.ToString("h:mm tt").ToLower().Replace(" ", "");
            var minutesToClose = GetMinutesToClose(currentTime, closeTime);
            
            string statusText;
            if (minutesToClose <= 60 && minutesToClose > 0)
            {
                statusText = $"Abierto · Cierra pronto ({closingTimeFormatted})";
            }
            else
            {
                statusText = $"Abierto · Cierra a las {closingTimeFormatted}";
            }

            return new OpenStatusResult(
                IsOpenNow: true,
                ClosingTime: closingTimeFormatted,
                NextOpenTime: null,
                OpenStatusText: statusText
            );
        }
        else
        {
            // Not open yet today, or already closed
            string? nextOpenTime;
            string statusText;

            if (currentTime < openTime)
            {
                // Not open yet today
                nextOpenTime = openTime.ToString("h:mm tt").ToLower().Replace(" ", "");
                statusText = $"Cerrado · Abre hoy a las {nextOpenTime}";
            }
            else
            {
                // Already closed today, find next open time
                var (nextOpenDay, nextOpenSchedule) = FindNextOpenDay(hours, currentDay, skipToday: true);
                nextOpenTime = FormatNextOpenTime(nextOpenDay, nextOpenSchedule, currentDay);
                statusText = nextOpenTime != null ? $"Cerrado · Abre {nextOpenTime}" : "Cerrado";
            }

            return new OpenStatusResult(
                IsOpenNow: false,
                ClosingTime: null,
                NextOpenTime: nextOpenTime,
                OpenStatusText: statusText
            );
        }
    }

    private static bool IsTimeWithinRange(TimeOnly currentTime, TimeOnly openTime, TimeOnly closeTime, DaySchedule schedule)
    {
        // Handle break times
        if (schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue)
        {
            // Check if in break time
            if (currentTime >= schedule.BreakStart.Value && currentTime < schedule.BreakEnd.Value)
            {
                return false;
            }
        }

        // Handle overnight hours (close time is before open time)
        if (closeTime < openTime)
        {
            // Open overnight: either after open or before close
            return currentTime >= openTime || currentTime < closeTime;
        }

        // Normal hours
        return currentTime >= openTime && currentTime < closeTime;
    }

    private static int GetMinutesToClose(TimeOnly currentTime, TimeOnly closeTime)
    {
        if (closeTime < currentTime)
        {
            // Overnight, closing is next day
            return (int)(TimeSpan.FromDays(1) - currentTime.ToTimeSpan() + closeTime.ToTimeSpan()).TotalMinutes;
        }
        return (int)(closeTime.ToTimeSpan() - currentTime.ToTimeSpan()).TotalMinutes;
    }

    private static DaySchedule GetScheduleForDay(BusinessHours hours, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => hours.Monday,
        DayOfWeek.Tuesday => hours.Tuesday,
        DayOfWeek.Wednesday => hours.Wednesday,
        DayOfWeek.Thursday => hours.Thursday,
        DayOfWeek.Friday => hours.Friday,
        DayOfWeek.Saturday => hours.Saturday,
        DayOfWeek.Sunday => hours.Sunday,
        _ => hours.Monday
    };

    private static (DayOfWeek day, DaySchedule schedule) FindNextOpenDay(BusinessHours hours, DayOfWeek currentDay, bool skipToday = false)
    {
        var startOffset = skipToday ? 1 : 0;
        
        for (int i = startOffset; i < 7; i++)
        {
            var day = (DayOfWeek)(((int)currentDay + i) % 7);
            var schedule = GetScheduleForDay(hours, day);
            
            if (!schedule.IsClosed && schedule.OpenTime.HasValue)
            {
                return (day, schedule);
            }
        }

        // No open days found (shouldn't happen with real data)
        return (currentDay, new DaySchedule { IsClosed = true });
    }

    private static string? FormatNextOpenTime(DayOfWeek nextOpenDay, DaySchedule schedule, DayOfWeek currentDay)
    {
        if (schedule.IsClosed || !schedule.OpenTime.HasValue)
        {
            return null;
        }

        var timeFormatted = schedule.OpenTime.Value.ToString("h:mm tt").ToLower().Replace(" ", "");
        var dayName = GetSpanishDayName(nextOpenDay);

        // Check if it's tomorrow
        var nextDayIndex = ((int)currentDay + 1) % 7;
        if ((int)nextOpenDay == nextDayIndex)
        {
            return $"mañana a las {timeFormatted}";
        }

        // Check if it's today (shouldn't happen with skipToday, but just in case)
        if (nextOpenDay == currentDay)
        {
            return $"hoy a las {timeFormatted}";
        }

        return $"el {dayName} a las {timeFormatted}";
    }

    private static string GetSpanishDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sábado",
        DayOfWeek.Sunday => "domingo",
        _ => "lunes"
    };
}
