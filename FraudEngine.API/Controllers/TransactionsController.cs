using System;
using System.Threading.Tasks;
using FraudEngine.Application.DTOs;
using FraudEngine.Application.Features.Transactions.Commands;
using FraudEngine.Application.Features.Transactions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FraudEngine.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> EvaluateTransaction([FromBody] TransactionDto dto)
    {
        var command = new EvaluateTransactionCommand(dto);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { Error = result.Error.Code, result.Error.Message });
        }

        return Ok(result.Value);
    }

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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var query = new GetTransactionByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return NotFound(new { Error = result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }
}
