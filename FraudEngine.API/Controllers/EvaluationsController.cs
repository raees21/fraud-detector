using FraudEngine.API.Auth;
using FraudEngine.Application.Features.Evaluations.Queries;
using FraudEngine.Application.DTOs;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FraudEngine.API.Controllers;

/// <summary>
/// Controller for querying fraud evaluation historical data.
/// </summary>
public class EvaluationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationsController"/> class.
    /// </summary>
    public EvaluationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves a paginated list of fraud evaluations.
    /// </summary>
    /// <param name="decision">Optional filter by decision outcome.</param>
    /// <param name="minScore">Optional lower bound for the risk score.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated list of fraud evaluation records.</returns>
    [HttpGet]
    [Authorize(Policy = ApiAuthorizationPolicies.ReadEvaluations)]
    public async Task<IActionResult> GetEvaluations(
        [FromQuery] Decision? decision,
        [FromQuery] int? minScore,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetEvaluationsQuery(decision, minScore, page, pageSize);
        Result<(IEnumerable<FraudEvaluation> Items, int TotalCount)> result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new
        {
            Data = result.Value.Items.Select(MapEvaluation),
            result.Value.TotalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    private static FraudEvaluationSummaryDto MapEvaluation(FraudEvaluation evaluation)
    {
        return new FraudEvaluationSummaryDto(
            evaluation.TransactionId,
            evaluation.Decision.ToString(),
            evaluation.EvaluatedAt
        );
    }
}
