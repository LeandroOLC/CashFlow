namespace CashFlow.Auth.API.Models;

public record RegisterRequest(string Email, string FullName, string Password, string ConfirmPassword);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string Token, string RefreshToken);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, string UserId, string Email);
