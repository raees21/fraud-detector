using FraudEngine.Application.Interfaces;
using StackExchange.Redis;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IVelocityService"/> using Redis to track transaction velocity.
/// </summary>
internal sealed class VelocityService : IVelocityService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _window = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Initializes a new instance of the <see cref="VelocityService"/> class.
    /// </summary>
    public VelocityService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task<int> GetRecentTransactionCountAsync(string accountId,
        CancellationToken cancellationToken = default)
    {
        IDatabase db = _redis.GetDatabase();
        string key = $"fraud:velocity:{accountId}";

        // INCR the key
        long count = await db.StringIncrementAsync(key);

        // Set expiration only if it's the first increment
        if (count == 1) await db.KeyExpireAsync(key, _window);

        return (int)count;
    }
}
