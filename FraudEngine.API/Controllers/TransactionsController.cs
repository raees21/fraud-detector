using FraudEngine.API.Auth;
using FraudEngine.Application.DTOs;
using FraudEngine.Application.Features.Transactions.Commands;
using FraudEngine.Application.Features.Transactions.Queries;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace FraudEngine.API.Controllers;

/// <summary>
/// Controller for managing and evaluating transactions.
/// </summary>
public class TransactionsController : ApiControllerBase
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
    /// Accepts a new transaction for asynchronous fraud evaluation.
    /// </summary>
    /// <param name="dto">The transaction details.</param>
    /// <returns>A submission receipt with the transaction identifier and pending status.</returns>
    [HttpPost]
    [Authorize(Policy = ApiAuthorizationPolicies.SubmitTransactions)]
    [EnableRateLimiting("transaction-submissions")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> SubmitTransaction([FromBody] TransactionDto dto)
    {
        var command = new SubmitTransactionCommand(dto);
        Result<TransactionSubmissionAcceptedDto> result = await _mediator.Send(command);

        if (result.IsFailure) return BadRequest(new { Error = result.Error.Code, result.Error.Message });

        return AcceptedAtAction(nameof(GetTransactionById), new { version = "1.0", id = result.Value.TransactionId },
            result.Value);
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
    [Authorize(Policy = ApiAuthorizationPolicies.ReadTransactions)]
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

        return Ok(new
        {
            Data = result.Value.Items.Select(MapTransaction),
            result.Value.TotalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Retrieves a specific transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <returns>The transaction details if found.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = ApiAuthorizationPolicies.ReadTransactions)]
    public async Task<IActionResult> GetTransactionById(Guid id)
    {
        var query = new GetTransactionByIdQuery(id);
        Result<TransactionStatusDto> result = await _mediator.Send(query);

        if (result.IsFailure)
            return NotFound(new { Error = result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }

    private static TransactionSummaryDto MapTransaction(Transaction transaction)
    {
        return new TransactionSummaryDto(
            transaction.Id,
            MaskAccountId(transaction.AccountId),
            transaction.Amount,
            transaction.Currency,
            transaction.MerchantName,
            transaction.MerchantCategory,
            transaction.TransactionType,
            transaction.ProcessingStatus.ToString(),
            transaction.Timestamp,
            transaction.CreatedAt
        );
    }

    private static string MaskAccountId(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            return "****";

        if (accountId.Length <= 4)
            return new string('*', accountId.Length);

        return $"{new string('*', accountId.Length - 4)}{accountId[^4..]}";
    }
}
