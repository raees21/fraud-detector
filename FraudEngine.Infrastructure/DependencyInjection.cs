using FraudEngine.Application.Interfaces;
using FraudEngine.Infrastructure.Persistence;
using FraudEngine.Infrastructure.Persistence.Repositories;
using FraudEngine.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FraudEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string? databaseConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(databaseConnectionString))
            throw new InvalidOperationException("A PostgreSQL connection string is required.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(databaseConnectionString));

        string? redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
            throw new InvalidOperationException("A Redis connection string is required.");

        ConfiguredIpLocationService.IpLocationOptions ipLocationOptions =
            BuildIpLocationOptions(configuration.GetSection("IpLocation"));
        services.AddSingleton<IOptions<ConfiguredIpLocationService.IpLocationOptions>>(
            Options.Create(ipLocationOptions));

        KafkaOptions kafkaOptions = BuildKafkaOptions(configuration.GetSection("Kafka"));
        services.AddSingleton<IOptions<KafkaOptions>>(Options.Create(kafkaOptions));

        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<ITransactionWorkflowRepository, TransactionWorkflowRepository>();

        services.AddScoped<IVelocityService, VelocityService>();
        services.AddSingleton<IIpLocationService, ConfiguredIpLocationService>();
        services.AddScoped<IRulesEngineService, RulesEngineService>();
        services.AddScoped<ITransactionEventProcessor, TransactionEventProcessor>();

        services.AddSingleton<IIntegrationEventTopicProvider, KafkaTopicProvider>();
        services.AddSingleton<IIntegrationEventProducer, KafkaIntegrationEventProducer>();
        services.AddSingleton<ITransactionSubmittedEventSubscriber, KafkaTransactionSubmittedEventSubscriber>();

        services.AddHostedService<OutboxPublisherHostedService>();
        services.AddHostedService<TransactionSubmittedConsumerHostedService>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddRedis(redisConnectionString);

        return services;
    }

    private static ConfiguredIpLocationService.IpLocationOptions BuildIpLocationOptions(IConfigurationSection section)
    {
        var options = new ConfiguredIpLocationService.IpLocationOptions();

        foreach (IConfigurationSection mappingSection in section.GetSection("Mappings").GetChildren())
        {
            options.Mappings.Add(new ConfiguredIpLocationService.IpLocationMappingOptions
            {
                Cidr = mappingSection["Cidr"] ?? string.Empty,
                CountryCode = mappingSection["CountryCode"] ?? string.Empty,
                CountryName = mappingSection["CountryName"] ?? string.Empty,
                IsReliable = bool.TryParse(mappingSection["IsReliable"], out bool isReliable) && isReliable
            });
        }

        return options;
    }

    private static KafkaOptions BuildKafkaOptions(IConfigurationSection section)
    {
        string? bootstrapServers = section["BootstrapServers"];
        string? consumerGroupId = section["ConsumerGroupId"];
        string? transactionSubmittedTopic = section["TransactionSubmittedTopic"];
        string? transactionEvaluatedTopic = section["TransactionEvaluatedTopic"];

        if (string.IsNullOrWhiteSpace(bootstrapServers) ||
            string.IsNullOrWhiteSpace(consumerGroupId) ||
            string.IsNullOrWhiteSpace(transactionSubmittedTopic) ||
            string.IsNullOrWhiteSpace(transactionEvaluatedTopic))
            throw new InvalidOperationException(
                "Kafka bootstrap servers, topics, and consumer group ID are required.");

        return new KafkaOptions
        {
            BootstrapServers = bootstrapServers,
            ConsumerGroupId = consumerGroupId,
            TransactionSubmittedTopic = transactionSubmittedTopic,
            TransactionEvaluatedTopic = transactionEvaluatedTopic,
            OutboxBatchSize = int.TryParse(section["OutboxBatchSize"], out int batchSize) ? batchSize : 20,
            IdleDelayMs = int.TryParse(section["IdleDelayMs"], out int idleDelayMs) ? idleDelayMs : 500
        };
    }
}
