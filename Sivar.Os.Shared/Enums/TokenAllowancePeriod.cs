namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Defines the period type for AI chat token allowances
/// The integer value represents the number of days in the period
/// </summary>
public enum TokenAllowancePeriod
{
    /// <summary>
    /// Token allowance resets daily (every 1 day)
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Token allowance resets weekly (every 7 days)
    /// </summary>
    Weekly = 7,

    /// <summary>
    /// Token allowance resets bi-weekly (every 14 days)
    /// </summary>
    BiWeekly = 14,

    /// <summary>
    /// Token allowance resets monthly (every 30 days)
    /// </summary>
    Monthly = 30
}
