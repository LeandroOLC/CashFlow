using Asp.Versioning;
using CashFlow.Shared.Models;
using CashFlow.Transactions.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Transactions.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/transaction-categories")]
public class TransactionCategoriesController(
    ITransactionCategoryService categoryService,
    ILogger<TransactionCategoriesController> logger) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items["CorrelationId"]?.ToString();

    /// <summary>Lista todas as categorias ativas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransactionCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        logger.LogInformation("GET all transaction categories requested");

        var result = await categoryService.GetAllAsync(ct);
        return Ok(ApiResponse<IEnumerable<TransactionCategoryDto>>.Ok(result, correlationId: CorrelationId));
    }

    /// <summary>Busca uma categoria pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        logger.LogInformation("GET transaction category {Id} requested", id);

        var result = await categoryService.GetByIdAsync(id, ct);

        return result is null
            ? NotFound(ApiResponse<string>.Fail($"Categoria {id} não encontrada.", correlationId: CorrelationId))
            : Ok(ApiResponse<TransactionCategoryDto>.Ok(result, correlationId: CorrelationId));
    }
}