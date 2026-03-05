using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Queries;

public record GetTransactionsQuery(string? Decision, string? AccountId, DateTimeOffset? From, DateTimeOffset? To, int Page, int PageSize) 
    : IRequest<Result<(IEnumerable<Transaction> Items, int TotalCount)>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, Result<(IEnumerable<Transaction> Items, int TotalCount)>>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionsQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<(IEnumerable<Transaction> Items, int TotalCount)>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetPagedAsync(request.Decision, request.AccountId, request.From, request.To, request.Page, request.PageSize, cancellationToken);
        return Result<(IEnumerable<Transaction> Items, int TotalCount)>.Success(result);
    }
}
