using Asp.Versioning;
using CashFlow.Shared.Models;
using CashFlow.Transactions.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Transactions.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public class TransactionsController(
    ITransactionService transactionService,
    ILogger<TransactionsController> logger) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items["CorrelationId"]?.ToString();

    private string UserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? "unknown";

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? type = null,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "GET /transactions page={Page} pageSize={PageSize} type={Type} user={UserId}",
            page, pageSize, type, UserId);

        var result = await transactionService.GetPagedAsync(
            page, pageSize, startDate, endDate, type, ct);

        return Ok(ApiResponse<PagedResponse<TransactionDto>>.Ok(
            result, correlationId: CorrelationId));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await transactionService.GetByIdAsync(id, ct);
        return result is null
            ? NotFound(ApiResponse<string>.Fail(
                $"Lançamento {id} não encontrado", correlationId: CorrelationId))
            : Ok(ApiResponse<TransactionDto>.Ok(result, correlationId: CorrelationId));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionDto dto, CancellationToken ct)
    {
        try
        {
            var result = await transactionService.CreateAsync(dto, UserId, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<TransactionDto>.Ok(result, correlationId: CorrelationId));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Validation error: {Message}", ex.Message);
            return BadRequest(ApiResponse<string>.Fail(
                ex.Message, correlationId: CorrelationId));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateTransactionDto dto, CancellationToken ct)
    {
        try
        {
            var result = await transactionService.UpdateAsync(id, dto, ct);
            return Ok(ApiResponse<TransactionDto>.Ok(result, correlationId: CorrelationId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(
                ex.Message, correlationId: CorrelationId));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(
                ex.Message, correlationId: CorrelationId));
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await transactionService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(
                ex.Message, correlationId: CorrelationId));
        }
    }
}