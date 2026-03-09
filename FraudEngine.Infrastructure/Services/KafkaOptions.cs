namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Configuration for Kafka topics and background processing.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Gets or sets the Kafka bootstrap servers.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the consumer group identifier.
    /// </summary>
    public string ConsumerGroupId { get; set; } = "fraud-engine";

    /// <summary>
    /// Gets or sets the submitted-transaction topic name.
    /// </summary>
    public string TransactionSubmittedTopic { get; set; } = "fraud.transactions.submitted";

    /// <summary>
    /// Gets or sets the evaluated-transaction topic name.
    /// </summary>
    public string TransactionEvaluatedTopic { get; set; } = "fraud.transactions.evaluated";

    /// <summary>
    /// Gets or sets the outbox publisher batch size.
    /// </summary>
    public int OutboxBatchSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the idle poll delay in milliseconds for background loops.
    /// </summary>
    public int IdleDelayMs { get; set; } = 500;
}
