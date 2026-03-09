using System.Runtime.CompilerServices;
using System.Text.Json;
using Confluent.Kafka;
using FraudEngine.Application.IntegrationEvents;
using FraudEngine.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Kafka-backed subscriber for submitted transaction events.
/// </summary>
internal sealed class KafkaTransactionSubmittedEventSubscriber : ITransactionSubmittedEventSubscriber
{
    private readonly ILogger<KafkaTransactionSubmittedEventSubscriber> _logger;
    private readonly KafkaOptions _options;

    public KafkaTransactionSubmittedEventSubscriber(
        IOptions<KafkaOptions> options,
        ILogger<KafkaTransactionSubmittedEventSubscriber> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<ReceivedIntegrationEvent> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(_options.TransactionSubmittedTopic);
        _logger.LogInformation("Subscribed to Kafka topic {Topic}", _options.TransactionSubmittedTopic);

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, string> result;
            try
            {
                result = consumer.Consume(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            Guid eventId = GetEventId(result.Message.Value);
            yield return new ReceivedIntegrationEvent(
                eventId,
                result.Topic,
                result.Message.Key ?? string.Empty,
                result.Message.Value,
                _ =>
                {
                    consumer.Commit(result);
                    return Task.CompletedTask;
                });

            await Task.Yield();
        }
    }

    private static Guid GetEventId(string payload)
    {
        try
        {
            TransactionSubmittedIntegrationEvent? integrationEvent =
                JsonSerializer.Deserialize<TransactionSubmittedIntegrationEvent>(payload);
            return integrationEvent?.EventId ?? Guid.Empty;
        }
        catch (JsonException)
        {
            return Guid.Empty;
        }
    }
}
