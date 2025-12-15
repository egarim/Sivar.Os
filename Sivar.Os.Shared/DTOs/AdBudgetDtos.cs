using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// DTO for profile ad settings and budget information
/// </summary>
public class ProfileAdSettingsDto
{
    /// <summary>
    /// Profile ID
    /// </summary>
    public Guid ProfileId { get; set; }

    /// <summary>
    /// Available ad credit balance (in USD)
    /// </summary>
    public decimal AdBudget { get; set; }

    /// <summary>
    /// Enable appearing as sponsored in search results
    /// </summary>
    public bool SponsoredEnabled { get; set; }

    /// <summary>
    /// Maximum amount willing to pay per click (CPC) in USD
    /// </summary>
    public decimal MaxBidPerClick { get; set; }

    /// <summary>
    /// Maximum daily spend limit in USD
    /// </summary>
    public decimal DailyAdLimit { get; set; }

    /// <summary>
    /// Amount spent today on ads
    /// </summary>
    public decimal AdSpentToday { get; set; }

    /// <summary>
    /// Total amount ever spent on ads
    /// </summary>
    public decimal TotalAdSpent { get; set; }

    /// <summary>
    /// Target keywords for ads (e.g., ["pizza", "restaurante"])
    /// Empty = appear for all relevant searches
    /// </summary>
    public List<string> AdTargetKeywords { get; set; } = new();

    /// <summary>
    /// Target radius in km for geo-targeting (0 = no geo restriction)
    /// </summary>
    public double AdTargetRadiusKm { get; set; }

    /// <summary>
    /// Total sponsored impressions
    /// </summary>
    public long SponsoredImpressions { get; set; }

    /// <summary>
    /// Total sponsored clicks received
    /// </summary>
    public long SponsoredClicks { get; set; }

    /// <summary>
    /// Click-through rate (clicks / impressions)
    /// </summary>
    public double SponsoredCtr { get; set; }

    /// <summary>
    /// Quality score based on CTR performance (0.1 to 1.0)
    /// </summary>
    public double AdQualityScore { get; set; }
}

/// <summary>
/// DTO for updating profile ad settings
/// </summary>
public class UpdateAdSettingsDto
{
    /// <summary>
    /// Enable appearing as sponsored in search results
    /// </summary>
    public bool SponsoredEnabled { get; set; }

    /// <summary>
    /// Maximum amount willing to pay per click (CPC) in USD
    /// Range: $0.05 - $5.00
    /// </summary>
    public decimal MaxBidPerClick { get; set; }

    /// <summary>
    /// Maximum daily spend limit in USD
    /// Range: $1.00 - $1000.00
    /// </summary>
    public decimal DailyAdLimit { get; set; }

    /// <summary>
    /// Target keywords for ads
    /// Empty = appear for all relevant searches
    /// </summary>
    public List<string> AdTargetKeywords { get; set; } = new();

    /// <summary>
    /// Target radius in km for geo-targeting (0 = no geo restriction)
    /// Range: 0 - 100 km
    /// </summary>
    public double AdTargetRadiusKm { get; set; }
}

/// <summary>
/// DTO for ad transaction history item
/// </summary>
public class AdTransactionDto
{
    /// <summary>
    /// Transaction ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of transaction
    /// </summary>
    public AdTransactionType TransactionType { get; set; }

    /// <summary>
    /// Formatted transaction type name
    /// </summary>
    public string TransactionTypeName => TransactionType.ToString();

    /// <summary>
    /// Amount of the transaction (positive = credit, negative = debit)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Balance after this transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Timestamp of the transaction
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Description of the transaction
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// DTO for adding budget to a profile
/// </summary>
public class AddBudgetDto
{
    /// <summary>
    /// Amount to add (must be positive)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Description/reason for adding budget
    /// </summary>
    public string? Description { get; set; }
}
