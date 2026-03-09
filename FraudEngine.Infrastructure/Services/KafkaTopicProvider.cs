using FraudEngine.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Provides Kafka topic names to application services.
/// </summary>
internal sealed class KafkaTopicProvider : IIntegrationEventTopicProvider
{
    private readonly KafkaOptions _options;

    public KafkaTopicProvider(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
    }

    public string TransactionSubmittedTopic => _options.TransactionSubmittedTopic;

    public string TransactionEvaluatedTopic => _options.TransactionEvaluatedTopic;
}
