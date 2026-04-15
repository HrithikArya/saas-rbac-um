using System.Collections.Concurrent;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// In-process token store backed by a ConcurrentDictionary.
/// Use in development when Redis is not available.
/// NOT suitable for production multi-instance deployments — tokens are node-local.
/// </summary>
public class InMemoryTokenStore : ITokenStore
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime Expiry)> _store = new();
    private readonly ILogger<InMemoryTokenStore> _logger;

    public InMemoryTokenStore(ILogger<InMemoryTokenStore> logger)
    {
        _logger = logger;
        _logger.LogWarning(
            "Using InMemoryTokenStore — tokens are not shared across processes. " +
            "Configure REDIS_URL for production use.");
    }

    public Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
    {
        _store[key] = (value, DateTime.UtcNow.Add(expiry));
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.Expiry > DateTime.UtcNow)
                return Task.FromResult<string?>(entry.Value);

            // Expired — evict lazily
            _store.TryRemove(key, out _);
        }
        return Task.FromResult<string?>(null);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
