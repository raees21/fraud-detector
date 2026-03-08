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

        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();

        services.AddScoped<IVelocityService, VelocityService>();
        services.AddSingleton<IIpLocationService, ConfiguredIpLocationService>();
        services.AddScoped<IRulesEngineService, RulesEngineService>();

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
}
