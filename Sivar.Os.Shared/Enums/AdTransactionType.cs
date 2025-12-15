namespace Sivar.Os.Shared.Enums;

/// <summary>
/// Types of ad budget transactions
/// </summary>
public enum AdTransactionType
{
    /// <summary>
    /// Budget added via payment or promotion
    /// </summary>
    TopUp = 1,

    /// <summary>
    /// Budget deducted for a sponsored click
    /// </summary>
    Click = 2,

    /// <summary>
    /// Budget refunded (fraud detection, error, etc.)
    /// </summary>
    Refund = 3,

    /// <summary>
    /// Manual adjustment by admin
    /// </summary>
    Adjustment = 4,

    /// <summary>
    /// Promotional credit (bonus, welcome offer, etc.)
    /// </summary>
    Bonus = 5
}
