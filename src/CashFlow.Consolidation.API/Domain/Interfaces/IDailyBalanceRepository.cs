using CashFlow.Consolidation.API.Domain.Entities;

namespace CashFlow.Consolidation.API.Domain.Interfaces;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(DateTime date, CancellationToken ct = default);
    Task<DailyBalance?> GetLatestAsync(CancellationToken ct = default);
    Task AddAsync(DailyBalance entity, CancellationToken ct = default);
    Task<(IEnumerable<DailyBalance> Items, int Total)> GetByPeriodPagedAsync(
            DateTime startDate, DateTime endDate,
            int page, int pageSize,
            CancellationToken ct = default);
    void Update(DailyBalance entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
