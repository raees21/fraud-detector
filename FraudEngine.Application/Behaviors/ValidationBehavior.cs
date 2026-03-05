using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FraudEngine.Domain.Common;
using MediatR;

namespace FraudEngine.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            // Reflection hack to return Result<T>.Failure or Result.Failure safely.
            // In a production app you'd use a more robust way, but this works for Result<T>.
            var error = new Error("Validation.Error", string.Join("; ", failures.Select(f => f.ErrorMessage)));
            
            if (typeof(TResponse).IsGenericType && 
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod("Failure", new[] { typeof(Error) });
                
                return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
            }
            else if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }
        }

        return await next();
    }
}
