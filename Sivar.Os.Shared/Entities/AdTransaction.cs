using System.ComponentModel.DataAnnotations;
using Sivar.Os.Shared.Enums;

namespace Sivar.Os.Shared.Entities;

/// <summary>
/// Transaction record for ad budget changes (top-ups, clicks, refunds)
/// Provides audit trail for all budget modifications
/// </summary>
public class AdTransaction : BaseEntity
{
    /// <summary>
    /// The profile whose budget was affected
    /// </summary>
    public virtual Guid ProfileId { get; set; }
    public virtual Profile Profile { get; set; } = null!;

    /// <summary>
    /// Type of transaction
    /// </summary>
    public virtual AdTransactionType TransactionType { get; set; }

    /// <summary>
    /// Amount of the transaction (positive = credit, negative = debit)
    /// </summary>
    public virtual decimal Amount { get; set; }

    /// <summary>
    /// Balance after this transaction
    /// </summary>
    public virtual decimal BalanceAfter { get; set; }

    /// <summary>
    /// User who triggered the transaction (for clicks: the user who clicked)
    /// </summary>
    public virtual Guid? TriggeredByUserId { get; set; }

    /// <summary>
    /// Timestamp of the transaction
    /// </summary>
    public virtual DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Description of the transaction
    /// </summary>
    [StringLength(500)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// External reference (payment ID, search query, etc.)
    /// </summary>
    [StringLength(200)]
    public virtual string? ExternalReference { get; set; }

    /// <summary>
    /// Search query that triggered the click (for Click transactions)
    /// </summary>
    [StringLength(500)]
    public virtual string? SearchQuery { get; set; }

    /// <summary>
    /// Position in search results when clicked (for Click transactions)
    /// </summary>
    public virtual int? SearchPosition { get; set; }

    /// <summary>
    /// IP address for fraud detection (hashed for privacy)
    /// </summary>
    [StringLength(64)]
    public virtual string? IpAddressHash { get; set; }
}
