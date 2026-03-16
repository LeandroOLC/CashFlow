using CashFlow.Shared.Models;

namespace CashFlow.Consolidation.API.Services;

public record DailyBalanceDto(
    Guid Id, DateTime Date,
    decimal TotalCredits, decimal TotalDebits, decimal FinalBalance,
    DateTime LastUpdated);

public interface IConsolidationService
{
    Task ProcessTransactionCreatedAsync(DateTime date, decimal amount, string type, CancellationToken ct = default);
    Task ProcessTransactionUpdatedAsync(
        decimal oldAmount, string oldType, DateTime oldDate,
        decimal newAmount, string newType, DateTime newDate,
        CancellationToken ct = default);
    Task ProcessTransactionDeletedAsync(DateTime date, decimal amount, string type, CancellationToken ct = default);

    Task<DailyBalanceDto?> GetByDateAsync(DateTime date, CancellationToken ct = default);
    Task<DailyBalanceDto?> GetLatestAsync(CancellationToken ct = default);

    /// <summary>Retorna saldo consolidado por período com paginação.</summary>
    Task<PagedResponse<DailyBalanceDto>> GetByPeriodAsync(
        DateTime startDate, DateTime endDate,
        int page, int pageSize,
        CancellationToken ct = default);
}

