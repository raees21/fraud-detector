using System;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.DTOs;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Commands;

public class EvaluateTransactionCommandHandler : IRequestHandler<EvaluateTransactionCommand, Result<FraudEvaluationResultDto>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IRulesEngineService _rulesEngineService;

    public EvaluateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IEvaluationRepository evaluationRepository,
        IRulesEngineService rulesEngineService)
    {
        _transactionRepository = transactionRepository;
        _evaluationRepository = evaluationRepository;
        _rulesEngineService = rulesEngineService;
    }

    public async Task<Result<FraudEvaluationResultDto>> Handle(EvaluateTransactionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Transaction;
        var transaction = new Transaction
        {
            AccountId = dto.AccountId,
            Amount = dto.Amount,
            Currency = dto.Currency,
            MerchantName = dto.MerchantName,
            MerchantCategory = dto.MerchantCategory,
            IPAddress = dto.IPAddress,
            DeviceId = dto.DeviceId,
            AccountAgeDays = dto.AccountAgeDays,
            Timestamp = dto.Timestamp
        };

        // Save transaction
        await _transactionRepository.AddAsync(transaction, cancellationToken);
        
        // Evaluate rules
        var (score, triggeredRules, decision) = await _rulesEngineService.EvaluateAsync(transaction, cancellationToken);

        var evaluation = new FraudEvaluation
        {
            TransactionId = transaction.Id,
            RiskScore = score,
            Decision = decision,
            TriggeredRules = triggeredRules
        };

        // Save evaluation
        await _evaluationRepository.AddAsync(evaluation, cancellationToken);

        return Result<FraudEvaluationResultDto>.Success(new FraudEvaluationResultDto(
            transaction.Id,
            evaluation.RiskScore,
            evaluation.Decision.ToString(),
            evaluation.TriggeredRules
        ));
    }
}
