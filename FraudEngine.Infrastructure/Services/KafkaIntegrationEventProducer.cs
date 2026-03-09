using Confluent.Kafka;
using FraudEngine.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Kafka-backed implementation of <see cref="IIntegrationEventProducer"/>.
/// </summary>
internal sealed class KafkaIntegrationEventProducer : IIntegrationEventProducer, IDisposable
{
    private readonly ILogger<KafkaIntegrationEventProducer> _logger;
    private readonly IProducer<string, string> _producer;

    public KafkaIntegrationEventProducer(IOptions<KafkaOptions> options, ILogger<KafkaIntegrationEventProducer> logger)
    {
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();
    }

    public async Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default)
    {
        DeliveryResult<string, string> result = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = payload
        }, cancellationToken);

        _logger.LogInformation("Published integration event to topic {Topic} at offset {Offset}", result.Topic,
            result.Offset.Value);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
