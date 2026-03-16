using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using CashFlow.BlazorWasm.Models;

namespace CashFlow.BlazorWasm.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(LoginRequest request);
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetUserEmailAsync();
}

public class AuthService(
    IHttpClientFactory factory,
    ILocalStorageService storage) : IAuthService
{
    private HttpClient Http => factory.CreateClient("auth");

    private const string TokenKey = "cf_token";
    private const string RefreshKey = "cf_refresh";
    private const string EmailKey = "cf_email";

    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync("/api/v1/auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                return (false, err?.Message ?? "Credenciais inválidas.");
            }
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
            if (result?.Data is null) return (false, "Resposta inválida do servidor.");

            await storage.SetItemAsStringAsync(TokenKey, result.Data.AccessToken);
            await storage.SetItemAsStringAsync(RefreshKey, result.Data.RefreshToken);
            await storage.SetItemAsStringAsync(EmailKey, result.Data.Email);
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await Http.PostAsJsonAsync("/api/v1/auth/register", request);
            if (response.IsSuccessStatusCode) return (true, null);
            var err = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
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