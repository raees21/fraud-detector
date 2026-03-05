using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using MediatR;

namespace FraudEngine.Application.Features.Evaluations.Queries;

public record GetEvaluationsQuery(Decision? Decision, int? MinScore, int Page, int PageSize) 
    : IRequest<Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>>;

public class GetEvaluationsQueryHandler : IRequestHandler<GetEvaluationsQuery, Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>>
{
    private readonly IEvaluationRepository _repository;

    public GetEvaluationsQueryHandler(IEvaluationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>> Handle(GetEvaluationsQuery request, CancellationToken cancellationToken)
    {
        var result = await _repository.GetPagedAsync(request.Decision, request.MinScore, request.Page, request.PageSize, cancellationToken);
        return Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>.Success(result);
    }
}
