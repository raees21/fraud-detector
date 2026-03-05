using FraudEngine.Application.DTOs;
using FraudEngine.Application.Features.Transactions.Commands;
using FraudEngine.Application.Features.Transactions.Queries;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.API.Controllers;

/// <summary>
/// Controller for managing and evaluating transactions.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Evaluates a new transaction against the active fraud rules.
    /// </summary>
    /// <param name="dto">The transaction details.</param>
    /// <returns>The evaluation result with risk score and decision.</returns>
    [HttpPost]
    public async Task<IActionResult> EvaluateTransaction([FromBody] TransactionDto dto)
    {
        var command = new EvaluateTransactionCommand(dto);
        Result<FraudEvaluationResultDto> result = await _mediator.Send(command);

        if (result.IsFailure) return BadRequest(new { Error = result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves a paginated list of historical transactions.
    /// </summary>
    /// <param name="decision">Optional filter by the evaluation decision (e.g., ALLOW, REVIEW, BLOCK).</param>
    /// <param name="accountId">Optional filter by account ID.</param>
    /// <param name="from">Optional start date for the transaction timestamp.</param>
    /// <param name="to">Optional end date for the transaction timestamp.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated list of transactions.</returns>
    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? decision,
        [FromQuery] string? accountId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetTransactionsQuery(decision, accountId, from, to, page, pageSize);
        Result<(IEnumerable<Transaction> Items, int TotalCount)> result = await _mediator.Send(query);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(new { Data = result.Value.Items, result.Value.TotalCount, Page = page, PageSize = pageSize });
    }

    /// <summary>
    /// Retrieves a specific transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <returns>The transaction details if found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var query = new GetTransactionByIdQuery(id);
        Result<Transaction> result = await _mediator.Send(query);

        if (result.IsFailure)
            return NotFound(new { Error = result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }
}
