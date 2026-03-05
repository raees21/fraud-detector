using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;

namespace FraudEngine.Application.Interfaces;

public interface IEvaluationRepository
{
    Task AddAsync(FraudEvaluation evaluation, CancellationToken cancellationToken = default);
    Task<(IEnumerable<FraudEvaluation> Items, int TotalCount)> GetPagedAsync(
        Decision? decision, int? minScore, int page, int pageSize, CancellationToken cancellationToken = default);
}
