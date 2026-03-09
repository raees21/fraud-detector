using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of <see cref="IOutboxRepository"/> using Entity Framework Core.
/// </summary>
internal sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;

    public OutboxRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .AsNoTracking()
            .Where(message =>
                message.Status == OutboxMessageStatus.PENDING ||
                message.Status == OutboxMessageStatus.FAILED)
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkPublishedAsync(Guid outboxMessageId, DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default)
    {
        OutboxMessage? message = await _context.OutboxMessages
            .FirstOrDefaultAsync(item => item.Id == outboxMessageId, cancellationToken);
        if (message is null)
            return;

        message.Status = OutboxMessageStatus.PUBLISHED;
        message.PublishedAt = publishedAt;
        message.LastError = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid outboxMessageId, string error, CancellationToken cancellationToken = default)
    {
        OutboxMessage? message = await _context.OutboxMessages
            .FirstOrDefaultAsync(item => item.Id == outboxMessageId, cancellationToken);
        if (message is null)
            return;

        message.Status = OutboxMessageStatus.FAILED;
        message.Attempts += 1;
        message.LastError = error.Length <= 2000 ? error : error[..2000];
        await _context.SaveChangesAsync(cancellationToken);
    }
}
