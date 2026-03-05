using System;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Queries;

public record GetTransactionByIdQuery(Guid Id) : IRequest<Result<Transaction>>;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, Result<Transaction>>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Transaction>> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (transaction == null)
            return Result<Transaction>.Failure(new Error("Transaction.NotFound", "Transaction with the given ID was not found."));

        return Result<Transaction>.Success(transaction);
    }
}
