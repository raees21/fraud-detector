using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;

namespace FraudEngine.Application.Features.Rules.Queries
{
    public record GetRulesQuery : IRequest<Result<IEnumerable<RuleDefinition>>>;

    public class GetRulesQueryHandler : IRequestHandler<GetRulesQuery, Result<IEnumerable<RuleDefinition>>>
    {
        private readonly IRuleRepository _repository;

        public GetRulesQueryHandler(IRuleRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IEnumerable<RuleDefinition>>> Handle(GetRulesQuery request, CancellationToken cancellationToken)
        {
            var rules = await _repository.GetAllAsync(cancellationToken);
            return Result<IEnumerable<RuleDefinition>>.Success(rules);
        }
    }
}

namespace FraudEngine.Application.Features.Rules.Commands
{
    public record ToggleRuleCommand(Guid Id) : IRequest<Result<bool>>;

    public class ToggleRuleCommandHandler : IRequestHandler<ToggleRuleCommand, Result<bool>>
    {
        private readonly IRuleRepository _repository;
        private readonly IRulesEngineService _rulesEngineService;

        public ToggleRuleCommandHandler(IRuleRepository repository, IRulesEngineService rulesEngineService)
        {
            _repository = repository;
            _rulesEngineService = rulesEngineService;
        }

        public async Task<Result<bool>> Handle(ToggleRuleCommand request, CancellationToken cancellationToken)
        {
            var rule = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (rule == null)
                return Result<bool>.Failure(new Error("Rule.NotFound", "Rule not found."));

            rule.IsActive = !rule.IsActive;
            await _repository.UpdateAsync(rule, cancellationToken);

            // Reload rules in engine to reflect changes immediately
            await _rulesEngineService.ReloadRulesAsync(cancellationToken);

            return Result<bool>.Success(rule.IsActive);
        }
    }
}
