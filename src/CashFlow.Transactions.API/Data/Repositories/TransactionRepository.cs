using CashFlow.Transactions.API.Domain.Entities;
using CashFlow.Transactions.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.API.Data.Repositories;

public class TransactionRepository(TransactionDbContext context, ILogger<TransactionRepository> logger)
    : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching transaction {Id}", id);
        return await context.Transactions.FindAsync([id], ct);
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken ct = default) =>
        await context.Transactions.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Transaction entity, CancellationToken ct = default)
    {
        logger.LogDebug("Adding transaction {Id}", entity.Id);
        await context.Transactions.AddAsync(entity, ct);
    }

    public void Update(Transaction entity)
    {
        logger.LogDebug("Updating transaction {Id}", entity.Id);
        context.Transactions.Update(entity);
    }

    public void Remove(Transaction entity)
    {
        logger.LogDebug("Removing transaction {Id}", entity.Id);
        context.Transactions.Remove(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default) =>
        await context.Transactions
            .AsNoTracking()
            .Where(t => t.Date >= start.Date && t.Date <= end.Date)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);

    public async Task<IEnumerable<Transaction>> GetByDateAsync(DateTime date, CancellationToken ct = default) =>
        await context.Transactions
            .AsNoTracking()
            .Where(t => t.Date == date.Date)
            .ToListAsync(ct);

    public async Task<(IEnumerable<Transaction> Items, int Total)> GetPagedAsync(
        int page, int pageSize, DateTime? startDate, DateTime? endDate,
        TransactionType? type, CancellationToken ct = default)
    {
        var query = context.Transactions.AsNoTracking().AsQueryable();

        if (startDate.HasValue) query = query.Where(t => t.Date >= startDate.Value.Date);
        if (endDate.HasValue) query = query.Where(t => t.Date <= endDate.Value.Date);
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
