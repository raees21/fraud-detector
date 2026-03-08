namespace FraudEngine.Domain.Enums;

/// <summary>
/// Represents the type of transaction being evaluated.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Unknown or unspecified transaction type.
    /// </summary>
    UNKNOWN = 0,

    /// <summary>
    /// Electronic funds transfer.
    /// </summary>
    EFT = 1,

    /// <summary>
    /// Card-based transaction.
    /// </summary>
    CARD = 2,

    /// <summary>
    /// Automated or recurring payment.
    /// </summary>
    AUTOMATED_OR_RECURRING = 3,

    /// <summary>
    /// Mobile-initiated transaction.
    /// </summary>
    MOBILE = 4,

    /// <summary>
    /// E-wallet transaction.
    /// </summary>
    EWALLET = 5
}
