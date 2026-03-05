using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Queries;

/// <summary>
/// Query to retrieve a specific transaction by its unique identifier.
/// </summary>
public record GetTransactionByIdQuery(Guid Id) : IRequest<Result<Transaction>>;

/// <summary>
/// Handler for the <see cref="GetTransactionByIdQuery"/>.
/// </summary>
public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, Result<Transaction>>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Handles the query to retrieve a transaction by ID.
    /// </summary>
    public async Task<Result<Transaction>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        Transaction? transaction = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (transaction == null)
            return Result<Transaction>.Failure(new Error("Transaction.NotFound",
                "Transaction with the given ID was not found."));

        return Result<Transaction>.Success(transaction);
    }
}
