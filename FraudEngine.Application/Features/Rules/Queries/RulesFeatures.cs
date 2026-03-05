using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;

namespace FraudEngine.Application.Features.Rules.Queries
{
    /// <summary>
    /// Query to retrieve all rule definitions.
    /// </summary>
    public record GetRulesQuery : IRequest<Result<IEnumerable<RuleDefinition>>>;

    /// <summary>
    /// Handler for the <see cref="GetRulesQuery"/>.
    /// </summary>
    public class GetRulesQueryHandler : IRequestHandler<GetRulesQuery, Result<IEnumerable<RuleDefinition>>>
    {
        private readonly IRuleRepository _repository;

        public GetRulesQueryHandler(IRuleRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Handles the query to retrieve all rules.
        /// </summary>
        public async Task<Result<IEnumerable<RuleDefinition>>> Handle(GetRulesQuery request,
            CancellationToken cancellationToken)
        {
            IEnumerable<RuleDefinition> rules = await _repository.GetAllAsync(cancellationToken);
            return Result<IEnumerable<RuleDefinition>>.Success(rules);
        }
    }
}

namespace FraudEngine.Application.Features.Rules.Commands
{
    /// <summary>
    /// Command to toggle the active status of a specific rule.
    /// </summary>
    public record ToggleRuleCommand(Guid Id) : IRequest<Result<bool>>;

    /// <summary>
    /// Handler for the <see cref="ToggleRuleCommand"/>.
    /// </summary>
    public class ToggleRuleCommandHandler : IRequestHandler<ToggleRuleCommand, Result<bool>>
    {
        private readonly IRuleRepository _repository;
        private readonly IRulesEngineService _rulesEngineService;

        public ToggleRuleCommandHandler(IRuleRepository repository, IRulesEngineService rulesEngineService)
        {
            _repository = repository;
            _rulesEngineService = rulesEngineService;
        }

        /// <summary>
        /// Handles the command to toggle a rule's status.
        /// </summary>
        public async Task<Result<bool>> Handle(ToggleRuleCommand request, CancellationToken cancellationToken)
        {
            RuleDefinition? rule = await _repository.GetByIdAsync(request.Id, cancellationToken);
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
