namespace Application.Common.Interfaces;

public interface ITokenStore
{
    Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
