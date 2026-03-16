using Asp.Versioning;
using CashFlow.Consolidation.API.Services;
using CashFlow.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace CashFlow.Consolidation.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/daily-consolidation")]
[Produces("application/json")]
public class DailyConsolidationController(
    IConsolidationService consolidationService,
    ILogger<DailyConsolidationController> logger) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items["CorrelationId"]?.ToString();

    // ── GET /daily-consolidation/latest ──────────────────────────────────────

    /// <summary>Retorna o saldo consolidado mais recente disponível.</summary>
    [HttpGet("latest")]
    [OutputCache(PolicyName = "consolidation")]
    [ProducesResponseType(typeof(ApiResponse<DailyBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        logger.LogInformation("GET daily-consolidation/latest");

        var result = await consolidationService.GetLatestAsync(ct);

        return result is null
            ? NotFound(ApiResponse<string>.Fail(
                "Nenhum dado de consolidação encontrado.",
                correlationId: CorrelationId))
            : Ok(ApiResponse<DailyBalanceDto>.Ok(result, correlationId: CorrelationId));
    }

    // ── GET /daily-consolidation/period ──────────────────────────────────────

    /// <summary>
    /// Retorna saldo consolidado por período com paginação.
    /// Limite de pageSize: 1–366 (máximo de 1 ano por página).
    /// </summary>
    [HttpGet("period")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DailyBalanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByPeriod(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 31,
        CancellationToken ct = default)
    {
        if (startDate.Date > endDate.Date)
            return BadRequest(ApiResponse<string>.Fail(
                "startDate deve ser anterior ou igual a endDate.",
                correlationId: CorrelationId));

        if (page < 1)
            return BadRequest(ApiResponse<string>.Fail(
                "page deve ser maior ou igual a 1.",
                correlationId: CorrelationId));

        if (pageSize is < 1 or > 366)
            return BadRequest(ApiResponse<string>.Fail(
                "pageSize deve estar entre 1 e 366.",
                correlationId: CorrelationId));

        logger.LogInformation(
            "GET daily-consolidation/period {Start}→{End} page={Page} pageSize={PageSize}",
            startDate.Date, endDate.Date, page, pageSize);

        var result = await consolidationService.GetByPeriodAsync(
            startDate, endDate, page, pageSize, ct);

        return Ok(ApiResponse<PagedResponse<DailyBalanceDto>>.Ok(result, correlationId: CorrelationId));
    }

    // ── GET /daily-consolidation/{date} ──────────────────────────────────────

    /// <summary>Retorna o saldo consolidado de uma data específica.</summary>
    /// <param name="date">Data no formato yyyy-MM-dd.</param>
    [HttpGet("{date:datetime}")]
    [OutputCache(PolicyName = "consolidation")]
    [ProducesResponseType(typeof(ApiResponse<DailyBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDate(DateTime date, CancellationToken ct)
    {
        if (date.Date > DateTime.UtcNow.Date)
            return BadRequest(ApiResponse<string>.Fail(
                "Não é possível consultar datas futuras.",
                correlationId: CorrelationId));

        logger.LogInformation("GET daily-consolidation/{Date}", date.Date);

        var result = await consolidationService.GetByDateAsync(date, ct);

        return result is null
            ? NotFound(ApiResponse<string>.Fail(
                $"Nenhum consolidado encontrado para {date:yyyy-MM-dd}.",
                correlationId: CorrelationId))
            : Ok(ApiResponse<DailyBalanceDto>.Ok(result, correlationId: CorrelationId));
    }
}