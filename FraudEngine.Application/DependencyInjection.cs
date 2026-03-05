using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FraudEngine.Application;

/// <summary>
/// Provides extension methods for registering application layer dependencies.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR and other application layer services into the service collection.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration => { configuration.RegisterServicesFromAssembly(assembly); });

        return services;
    }
}
