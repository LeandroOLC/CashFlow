using Microsoft.Extensions.Caching.Memory;

namespace CashFlow.Auth.API.Services;

public class InMemoryRevokedTokenRepository(IMemoryCache cache) : IRevokedTokenRepository
{
    private const string Prefix = "revoked_jti_";

    public Task RevokeAsync(string jti, TimeSpan ttl, CancellationToken ct = default)
    {
        cache.Set($"{Prefix}{jti}", true, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default) =>
        Task.FromResult(cache.TryGetValue($"{Prefix}{jti}", out _));
}