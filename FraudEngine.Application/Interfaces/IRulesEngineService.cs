using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;

namespace FraudEngine.Application.Interfaces;

public interface IRulesEngineService
{
    Task ReloadRulesAsync(CancellationToken cancellationToken = default);
    Task<(int TotalScore, string TriggeredRulesJson, Decision Decision)> EvaluateAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
