using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using CashFlow.WebApp.Models;

namespace CashFlow.WebApp.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(LoginRequest request);
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<bool>    IsAuthenticatedAsync();
    Task<string?> GetUserEmailAsync();
}

public class AuthService(HttpClient http, ILocalStorageService storage) : IAuthService
{
    private const string TokenKey   = "cf_token";
    private const string RefreshKey = "cf_refresh";
    private const string EmailKey   = "cf_email";

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("api/v1/auth/login", request);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadFromJsonAsync<ApiResponse<string>>();
                return (false, err?.Message ?? "Credenciais inválidas.");
            }
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
            if (result?.Data is null) return (false, "Resposta inválida.");
            await storage.SetItemAsStringAsync(TokenKey,   result.Data.AccessToken);
            await storage.SetItemAsStringAsync(RefreshKey, result.Data.RefreshToken);
            await storage.SetItemAsStringAsync(EmailKey,   result.Data.Email);
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("api/v1/auth/register", request);
            if (resp.IsSuccessStatusCode) return (true, null);
            var err = await resp.Content.ReadFromJsonAsync<ApiResponse<string>>();
            return (false, err?.Message ?? "Erro ao registrar.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task LogoutAsync()
    {
        await storage.RemoveItemAsync(TokenKey);
        await storage.RemoveItemAsync(RefreshKey);
        await storage.RemoveItemAsync(EmailKey);
    }

    public async Task<string?> GetTokenAsync() =>
        await storage.GetItemAsStringAsync(TokenKey);

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return false;
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.ValidTo > DateTime.UtcNow.AddMinutes(-1);
        }
        catch { return false; }
    }

    public async Task<string?> GetUserEmailAsync() =>
        await storage.GetItemAsStringAsync(EmailKey);
}
