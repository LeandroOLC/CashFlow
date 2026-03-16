using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CashFlow.Shared.Resilience;

/// <summary>
/// Políticas Polly reutilizadas pelos 3 microsserviços.
/// Registradas como Singleton via AddResiliencePolicies().
/// </summary>
public sealed class ResiliencePolicies
{
    // ── Retry: 3 tentativas com backoff exponencial (2s, 4s, 8s) ─────────────
    public AsyncRetryPolicy DatabaseRetry { get; }

    // ── Circuit Breaker: abre após 5 falhas em 30s, fica aberto por 60s ──────
    public AsyncCircuitBreakerPolicy DatabaseCircuitBreaker { get; }

    // ── Wrap: retry dentro do circuit breaker ────────────────────────────────
    public AsyncPolicy DatabasePolicy { get; }

    public ResiliencePolicies(ILogger<ResiliencePolicies> logger)
    {
        DatabaseRetry = Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                    logger.LogWarning(
                        "Tentativa {Attempt}/3 ao banco em {Delay:0.0}s. Erro: {Message}",
                        attempt, delay.TotalSeconds, ex.Message));

        DatabaseCircuitBreaker = Policy
            .Handle<SqlException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(60),
                onBreak: (ex, duration) =>
                    logger.LogError(
                        "Circuit Breaker ABERTO por {Duration:0}s. Banco de dados inacessível. Erro: {Message}",
                        duration.TotalSeconds, ex.Message),
                onReset: () =>
                    logger.LogInformation("Circuit Breaker FECHADO. Conexão com banco restaurada."),
                onHalfOpen: () =>
                    logger.LogInformation("Circuit Breaker SEMI-ABERTO. Testando conexão com banco."));

        // Retry só executa se o circuito estiver fechado/semi-aberto
        DatabasePolicy = Policy.WrapAsync(DatabaseRetry, DatabaseCircuitBreaker);
    }
}