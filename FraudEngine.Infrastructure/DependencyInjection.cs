using FraudEngine.Application.Interfaces;
using FraudEngine.Infrastructure.Persistence;
using FraudEngine.Infrastructure.Persistence.Repositories;
using FraudEngine.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FraudEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        string? redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        }

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();

        services.AddScoped<IVelocityService, VelocityService>();
        services.AddScoped<IRulesEngineService, RulesEngineService>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>()
            .AddRedis(configuration.GetConnectionString("Redis") ?? "127.0.0.1:6379");

        return services;
    }
}
