namespace FraudEngine.Domain.Enums;

/// <summary>
/// Represents the final decision of a fraud evaluation.
/// </summary>
public enum Decision
{
    /// <summary>
    /// The transaction is considered safe and is allowed.
    /// </summary>
    ALLOW,

    /// <summary>
    /// The transaction is suspicious and requires manual review.
    /// </summary>
    REVIEW,

    /// <summary>
    /// The transaction is strongly suspected of fraud and is blocked.
    /// </summary>
    BLOCK
}
