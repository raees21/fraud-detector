using FluentValidation;
using FluentValidation.Results;
using FraudEngine.Domain.Common;
using MediatR;

namespace FraudEngine.Application.Behaviors;

/// <summary>
/// Executes FluentValidation validators before handlers to fail fast on invalid input.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        ValidationResult[] validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        string[] failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .Select(error => error.ErrorMessage)
            .Distinct()
            .ToArray();

        if (failures.Length == 0)
            return await next();

        var error = new Error("Validation.InvalidInput", string.Join("; ", failures));
        return CreateFailureResult(error);
    }

    private static TResponse CreateFailureResult(Error error)
    {
        var failureMethod = typeof(TResponse).GetMethod(
            nameof(Result.Failure),
            new[] { typeof(Error) });

        if (failureMethod is null)
            throw new InvalidOperationException($"No failure factory found for {typeof(TResponse).Name}.");

        object? result = failureMethod.Invoke(null, new object[] { error });
        return result is TResponse typedResult
            ? typedResult
            : throw new InvalidOperationException($"Failed to create {typeof(TResponse).Name} validation result.");
    }
}
