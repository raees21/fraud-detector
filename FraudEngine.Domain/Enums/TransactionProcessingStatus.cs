namespace FraudEngine.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of asynchronous fraud processing for a transaction.
/// </summary>
public enum TransactionProcessingStatus
{
    /// <summary>
    /// The transaction was accepted and is waiting to be processed.
    /// </summary>
    PENDING = 0,

    /// <summary>
    /// The transaction is actively being evaluated.
    /// </summary>
    PROCESSING = 1,

    /// <summary>
    /// The transaction evaluation completed successfully.
    /// </summary>
    COMPLETED = 2,

    /// <summary>
    /// The transaction evaluation failed.
    /// </summary>
    FAILED = 3
}
