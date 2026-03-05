using System.Threading.Tasks;
using FraudEngine.Application.Features.Evaluations.Queries;
using FraudEngine.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EvaluationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EvaluationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetEvaluations(
        [FromQuery] Decision? decision,
        [FromQuery] int? minScore,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetEvaluationsQuery(decision, minScore, page, pageSize);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new
        {
            Data = result.Value.Items,
            TotalCount = result.Value.TotalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}
