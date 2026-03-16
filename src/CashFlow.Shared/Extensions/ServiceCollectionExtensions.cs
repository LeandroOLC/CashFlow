
using CashFlow.Shared.Middleware;
using CashFlow.Shared.Resilience;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace CashFlow.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra Serilog lendo do appsettings.json.</summary>
    public static IServiceCollection AddSerilog(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        services.AddSerilog((serviceProvider, config) =>
            config
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", environment.ApplicationName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName));

        return services;
    }

    /// <summary>Registra as políticas Polly (retry + circuit breaker) como Singleton.</summary>
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        services.AddSingleton<ResiliencePolicies>();
        return services;
    }

    /// <summary>Injeta X-Correlation-Id em todo request e popula o LogContext.</summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}