namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Provides integration event topic names to application services.
/// </summary>
public interface IIntegrationEventTopicProvider
{
    /// <summary>
    /// Gets the topic used for submitted transactions.
    /// </summary>
    public string TransactionSubmittedTopic { get; }

    /// <summary>
    /// Gets the topic used for completed evaluations.
    /// </summary>
    public string TransactionEvaluatedTopic { get; }
}
