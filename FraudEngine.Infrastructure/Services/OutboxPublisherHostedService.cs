using FraudEngine.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Background service that publishes pending outbox messages to Kafka.
/// </summary>
internal sealed class OutboxPublisherHostedService : BackgroundService
{
    private readonly ILogger<OutboxPublisherHostedService> _logger;
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OutboxPublisherHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<OutboxPublisherHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            IOutboxRepository outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            IIntegrationEventProducer producer = scope.ServiceProvider.GetRequiredService<IIntegrationEventProducer>();

            IReadOnlyList<FraudEngine.Domain.Entities.OutboxMessage> pendingMessages =
                await outboxRepository.GetPendingAsync(_options.OutboxBatchSize, stoppingToken);

            if (pendingMessages.Count == 0)
            {
                await Task.Delay(_options.IdleDelayMs, stoppingToken);
                continue;
            }

            foreach (FraudEngine.Domain.Entities.OutboxMessage message in pendingMessages)
            {
                try
                {
                    await producer.PublishAsync(message.Topic, message.MessageKey, message.Payload, stoppingToken);
                    await outboxRepository.MarkPublishedAsync(message.Id, DateTimeOffset.UtcNow, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox message {MessageId} to topic {Topic}", message.Id,
                        message.Topic);
                    await outboxRepository.MarkFailedAsync(message.Id, ex.Message, stoppingToken);
                }
            }
        }
    }
}
