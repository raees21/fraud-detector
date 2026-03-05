using System;
using System.Threading.Tasks;
using FraudEngine.Application.Features.Rules.Commands;
using FraudEngine.Application.Features.Rules.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetRules()
    {
        var query = new GetRulesQuery();
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> ToggleRule(Guid id)
    {
        var command = new ToggleRuleCommand(id);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
            return NotFound(new { Error = result.Error.Code, result.Error.Message });

        return Ok(new { IsActive = result.Value });
    }
}
