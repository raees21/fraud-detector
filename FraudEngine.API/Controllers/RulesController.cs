using FraudEngine.API.Auth;
using FraudEngine.Application.DTOs;
using FraudEngine.Application.Features.Rules.Commands;
using FraudEngine.Application.Features.Rules.Queries;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FraudEngine.API.Controllers;

/// <summary>
/// Controller for managing fraud detection rules.
/// </summary>
public class RulesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesController"/> class.
    /// </summary>
    public RulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves all mapped rule definitions.
    /// </summary>
    /// <returns>A list of rules.</returns>
    [HttpGet]
    [Authorize(Policy = ApiAuthorizationPolicies.ReadRules)]
    public async Task<IActionResult> GetRules()
    {
        var query = new GetRulesQuery();
        Result<IEnumerable<RuleDefinition>> result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value.Select(MapRule));
    }

    /// <summary>
    /// Toggles the active status of a specific rule.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <returns>The updated active status of the rule.</returns>
    [HttpPatch("{id:guid}/toggle")]
    [Authorize(Policy = ApiAuthorizationPolicies.ManageRules)]
    public async Task<IActionResult> ToggleRule(Guid id)
    {
        var command = new ToggleRuleCommand(id);
        Result<bool> result = await _mediator.Send(command);

        if (result.IsFailure)
            return NotFound(new { Error = result.Error.Code, result.Error.Message });

        return Ok(new { IsActive = result.Value });
    }

    private static RuleSummaryDto MapRule(RuleDefinition rule)
    {
        return new RuleSummaryDto(rule.Id, rule.RuleName, rule.Description, rule.IsActive);
    }
}
