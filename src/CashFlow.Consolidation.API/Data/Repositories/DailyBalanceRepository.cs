using CashFlow.Consolidation.API.Domain.Entities;
using CashFlow.Consolidation.API.Domain.Interfaces;
using CashFlow.Shared.Resilience;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace CashFlow.Consolidation.API.Data.Repositories;

public class DailyBalanceRepository(
    ConsolidationDbContext context,
    ResiliencePolicies resilience,
    ILogger<DailyBalanceRepository> logger) : IDailyBalanceRepository
{
    // ── Leitura ───────────────────────────────────────────────────────────────
    public async Task<DailyBalance?> GetByDateAsync(DateTime date, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching DailyBalance for {Date}", date.Date);

        return await resilience.DatabasePolicy.ExecuteAsync(async () =>
            await context.DailyBalances
                .FirstOrDefaultAsync(d => d.Date == date.Date, ct));
    }

    public async Task<DailyBalance?> GetLatestAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Fetching latest DailyBalance");

        return await resilience.DatabasePolicy.ExecuteAsync(async () =>
            await context.DailyBalances
                .OrderByDescending(d => d.Date)
                .FirstOrDefaultAsync(ct));
    }

    public async Task<(IEnumerable<DailyBalance> Items, int Total)> GetByPeriodPagedAsync(
        DateTime startDate, DateTime endDate,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        logger.LogDebug(
            "Fetching DailyBalances from {Start} to {End} — page={Page} pageSize={PageSize}",
            startDate.Date, endDate.Date, page, pageSize);

        return await resilience.DatabasePolicy.ExecuteAsync(async () =>
        {
            var query = context.DailyBalances
                .AsNoTracking()
                .Where(d => d.Date >= startDate.Date && d.Date <= endDate.Date)
                .OrderBy(d => d.Date);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return ((IEnumerable<DailyBalance>)items, total);
        });
    }

    // ── Escrita ───────────────────────────────────────────────────────────────

    public async Task AddAsync(DailyBalance entity, CancellationToken ct = default)
    {
        logger.LogDebug("Adding DailyBalance for {Date}", entity.Date);
        await context.DailyBalances.AddAsync(entity, ct);
    }

    public void Update(DailyBalance entity)
    {
        logger.LogDebug("Updating DailyBalance for {Date}", entity.Date);
        context.DailyBalances.Update(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await resilience.DatabasePolicy.ExecuteAsync(async () =>
            await context.SaveChangesAsync(ct));
    }
}