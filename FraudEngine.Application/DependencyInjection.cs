using FluentValidation;
using FraudEngine.Application.Behaviors;
using FraudEngine.Application.Features.Evaluations.Queries;
using FraudEngine.Application.Features.Transactions.Commands;
using FraudEngine.Application.Features.Transactions.Queries;
using FraudEngine.Application.Validation;
using System.Reflection;
using MediatR;
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
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IValidator<EvaluateTransactionCommand>, EvaluateTransactionCommandValidator>();
        services.AddScoped<IValidator<GetTransactionsQuery>, GetTransactionsQueryValidator>();
        services.AddScoped<IValidator<GetEvaluationsQuery>, GetEvaluationsQueryValidator>();

        return services;
    }
}
