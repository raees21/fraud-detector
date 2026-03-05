using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using MediatR;

namespace FraudEngine.Application.Features.Evaluations.Queries;

/// <summary>
/// Query to retrieve a paginated list of fraud evaluations.
/// </summary>
public record GetEvaluationsQuery(Decision? Decision, int? MinScore, int Page, int PageSize)
    : IRequest<Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>>;

/// <summary>
/// Handler for the <see cref="GetEvaluationsQuery"/>.
/// </summary>
public class GetEvaluationsQueryHandler : IRequestHandler<GetEvaluationsQuery,
    Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>>
{
    private readonly IEvaluationRepository _repository;

    public GetEvaluationsQueryHandler(IEvaluationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Handles the query to retrieve evaluations.
    /// </summary>
    public async Task<Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>> Handle(GetEvaluationsQuery request,
        CancellationToken cancellationToken)
    {
        (IEnumerable<FraudEvaluation> Items, int TotalCount) result = await _repository.GetPagedAsync(request.Decision,
            request.MinScore, request.Page, request.PageSize, cancellationToken);
        return Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)>.Success(result);
    }
}
