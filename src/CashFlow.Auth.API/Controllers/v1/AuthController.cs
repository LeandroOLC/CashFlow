using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using CashFlow.Auth.API.Models;
using CashFlow.Auth.API.Services;
using CashFlow.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Auth.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    IRevokedTokenRepository revokedTokens,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items["CorrelationId"]?.ToString();

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        logger.LogInformation("Registering user {Email}", request.Email);

        if (request.Password != request.ConfirmPassword)
            return BadRequest(ApiResponse<string>.Fail("Passwords do not match", correlationId: CorrelationId));

        var user = new ApplicationUser
        { UserName = request.Email, Email = request.Email, FullName = request.FullName };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(ApiResponse<string>.Fail(
                "Registration failed", result.Errors.Select(e => e.Description), CorrelationId));

        logger.LogInformation("User {Email} registered successfully", request.Email);
        return CreatedAtAction(nameof(Register),
            ApiResponse<string>.Ok("User registered successfully", correlationId: CorrelationId));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("Login attempt for {Email}", request.Email);

        // ── Brute force: usa SignInManager com lockout ────────────────────────
        // lockoutOnFailure: true → conta bloqueada após N tentativas falhas
        var result = await signInManager.PasswordSignInAsync(
            request.Email, request.Password,
            isPersistent: false, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            logger.LogWarning("Account locked for {Email}", request.Email);
            return StatusCode(StatusCodes.Status423Locked,
                ApiResponse<string>.Fail(
                    "Conta bloqueada temporariamente por excesso de tentativas. Tente novamente em alguns minutos.",
                    correlationId: CorrelationId));
        }

        if (!result.Succeeded)
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(ApiResponse<string>.Fail("Credenciais inválidas.", correlationId: CorrelationId));
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Unauthorized(ApiResponse<string>.Fail("Conta desativada.", correlationId: CorrelationId));

        var tokenResponse = await tokenService.GenerateTokenAsync(user);
        return Ok(ApiResponse<TokenResponse>.Ok(tokenResponse, correlationId: CorrelationId));
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var tokenResponse = await tokenService.RefreshTokenAsync(request.Token, request.RefreshToken);
            return Ok(ApiResponse<TokenResponse>.Ok(tokenResponse, correlationId: CorrelationId));
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning("Refresh token failed: {Message}", ex.Message);
            return Unauthorized(ApiResponse<string>.Fail(ex.Message, correlationId: CorrelationId));
        }
    }

    /// <summary>
    /// Revoga o token JWT atual adicionando seu JTI à blacklist.
    /// O token permanece inválido até o TTL original expirar.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (jti is not null && expClaim is not null)
        {
            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim));
            var ttl = exp - DateTimeOffset.UtcNow;
            if (ttl > TimeSpan.Zero)
                await revokedTokens.RevokeAsync(jti, ttl);
        }

        logger.LogInformation("User {UserId} logged out", User.FindFirstValue("sub"));
        return NoContent();
    }
}