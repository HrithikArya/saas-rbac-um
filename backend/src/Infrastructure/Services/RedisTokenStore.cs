using Application.Common.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class RedisTokenStore : ITokenStore
{
    private readonly IDatabase _db;

    public RedisTokenStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
    {
        await _db.StringSetAsync(key, value, expiry);
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(key);
    }
}
