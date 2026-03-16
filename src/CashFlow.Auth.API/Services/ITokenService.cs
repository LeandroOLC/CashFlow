using CashFlow.Auth.API.Models;

namespace CashFlow.Auth.API.Services;

public interface ITokenService
{
    Task<TokenResponse> GenerateTokenAsync(ApplicationUser user);
    Task<TokenResponse> RefreshTokenAsync(string token, string refreshToken);
}
