namespace CashFlow.Auth.API.Services;

/// <summary>
/// Blacklist de tokens JWT revogados (logout).
/// Armazenado em memória com TTL igual ao tempo de expiração do token.
/// Em produção multi-instância, substitua por IDistributedCache (Redis).
/// </summary>
public interface IRevokedTokenRepository
{
    Task RevokeAsync(string jti, TimeSpan ttl, CancellationToken ct = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default);
}
