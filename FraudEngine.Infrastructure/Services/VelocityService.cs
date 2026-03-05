using System;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using StackExchange.Redis;

namespace FraudEngine.Infrastructure.Services;

public class VelocityService : IVelocityService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _window = TimeSpan.FromSeconds(60);

    public VelocityService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<int> GetRecentTransactionCountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = $"fraud:velocity:{accountId}";

        // INCR the key
        var count = await db.StringIncrementAsync(key);

        // Set expiration only if it's the first increment
        if (count == 1)
        {
            await db.KeyExpireAsync(key, _window);
        }

        return (int)count;
    }
}
