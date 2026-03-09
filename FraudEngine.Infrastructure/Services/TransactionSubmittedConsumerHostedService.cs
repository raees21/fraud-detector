using System.Text.Json;
using FraudEngine.Application.IntegrationEvents;
using FraudEngine.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Background service that consumes submitted transaction events and evaluates them asynchronously.
/// </summary>
internal sealed class TransactionSubmittedConsumerHostedService : BackgroundService
{
    private readonly ILogger<TransactionSubmittedConsumerHostedService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITransactionSubmittedEventSubscriber _subscriber;

    public TransactionSubmittedConsumerHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ITransactionSubmittedEventSubscriber subscriber,
        ILogger<TransactionSubmittedConsumerHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _subscriber = subscriber;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ReceivedIntegrationEvent receivedEvent in _subscriber.ReadAsync(stoppingToken))
        {
            try
            {
                TransactionSubmittedIntegrationEvent? integrationEvent =
                    JsonSerializer.Deserialize<TransactionSubmittedIntegrationEvent>(receivedEvent.Payload);
                if (integrationEvent is null)
                {
                    _logger.LogWarning("Skipping submitted transaction event with empty payload on topic {Topic}",
                        receivedEvent.Topic);
                    await receivedEvent.AcknowledgeAsync(stoppingToken);
                    continue;
                }

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                ITransactionEventProcessor processor =
                    scope.ServiceProvider.GetRequiredService<ITransactionEventProcessor>();

                await processor.ProcessAsync(integrationEvent, receivedEvent.Topic, stoppingToken);
                await receivedEvent.AcknowledgeAsync(stoppingToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize submitted transaction event payload");
                await receivedEvent.AcknowledgeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing a submitted transaction event");
                await receivedEvent.AcknowledgeAsync(stoppingToken);
            }
        }
    }
}
