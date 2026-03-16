using Asp.Versioning;
using CashFlow.Shared.Extensions;
using CashFlow.Transactions.API.Data;
using CashFlow.Transactions.API.Data.Repositories;
using CashFlow.Transactions.API.Domain.Interfaces;
using CashFlow.Transactions.API.Infrastructure.Messaging;
using CashFlow.Transactions.API.Services;
using HealthChecks.RabbitMQ;
using HealthChecks.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(builder.Configuration, builder.Environment);
builder.Services.AddResiliencePolicies();

// ── HTTPS ─────────────────────────────────────────────────────────────────────
builder.Services.AddHttpsRedirection(opts => opts.HttpsPort = 443);
builder.Services.AddHsts(opts =>
{
    opts.MaxAge = TimeSpan.FromDays(365);
    opts.IncludeSubDomains = true;
});

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5000", "http://localhost:5163", "https://localhost:7297"];

builder.Services.AddCors(opts =>
    opts.AddPolicy("CashFlowCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    }));

// ── Compressão de resposta ────────────────────────────────────────────────────
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;    // habilita mesmo em HTTPS (aceitável em redes internas)
    opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
        .Concat(["application/json"]);
});

// ── Cache — SLA: 50 req/s sem degradação ─────────────────────────────────────
// Categorias são dados estáticos: TTL de 1h
// Saldo diário: TTL de 30s (atualizado pelo consumer assíncrono)
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache(opts =>
{
    opts.AddPolicy("categories", p => p.Expire(TimeSpan.FromHours(1)).Tag("categories"));
    opts.AddPolicy("transactions", p => p.Expire(TimeSpan.FromSeconds(30)));
});

// ── Rate Limiting — SLA: máx 5% perda em pico de 50 req/s ───────────────────
// Token bucket: 60 tokens (capacidade), 50 tokens/s (reposição) por IP
builder.Services.AddRateLimiter(opts =>
{
    opts.AddTokenBucketLimiter("api", o =>
    {
        o.TokenLimit = 60;
        o.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        o.TokensPerPeriod = 50;
        o.AutoReplenishment = true;
        o.QueueLimit = 5;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.OnRejected = async (ctx, _) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "1";
        await ctx.HttpContext.Response.WriteAsync("Rate limit excedido. Tente novamente em instantes.");
    };
});

// ── EF Core ───────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<TransactionDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("TransactionDb"),
        sql => sql.CommandTimeout(30)));    // timeout de query: 30s

// ── JWT ───────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ── RabbitMQ ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost",
        Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest",
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// ── API Versioning ────────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(opts =>
{
    opts.DefaultApiVersion = new ApiVersion(1, 0);
    opts.AssumeDefaultVersionWhenUnspecified = true;
    opts.ReportApiVersions = true;
}).AddApiExplorer(opts =>
{
    opts.GroupNameFormat = "'v'VVV";
    opts.SubstituteApiVersionInUrl = true;
});

// ── DI ────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionCategoryRepository, TransactionCategoryRepository>();
builder.Services.AddScoped<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionCategoryService, TransactionCategoryService>();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CashFlow Transactions API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
            { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>()
    }});
});

// ── Health Checks — liveness + readiness ─────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("TransactionDb")!,
        name: "sqlserver", tags: ["ready", "db"])
    .AddRabbitMQ(
        sp => sp.GetRequiredService<IConnection>(),
        name: "rabbitmq", tags: ["ready", "messaging"]);

var app = builder.Build();

app.UseResponseCompression();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("RequestHost", http.Request.Host.Value ?? string.Empty);
        diag.Set("RequestScheme", http.Request.Scheme);
        diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
        diag.Set("UserId", http.User.FindFirst("sub")?.Value ?? "anonymous");
    };
});

app.UseCorrelationId();

// Swagger habilitado em Development OU quando Swagger:Enabled=true (ex: Docker Compose)
var swaggerEnabled = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// UseCors DEVE vir antes de UseHttpsRedirection
// O preflight OPTIONS precisa ser respondido antes de qualquer redirect
app.UseCors("CashFlowCors");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("api");

// ── Health: liveness e readiness ─────────────────────────────────────────────
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = hc => hc.Tags.Contains("ready") });
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    await db.Database.MigrateAsync();
}

try
{
    Log.Information("Iniciando CashFlow.Transactions.API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host encerrado inesperadamente");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}