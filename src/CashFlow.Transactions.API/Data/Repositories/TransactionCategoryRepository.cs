using CashFlow.Transactions.API.Domain.Entities;
using CashFlow.Transactions.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.API.Data.Repositories;

public class TransactionCategoryRepository(
    TransactionDbContext context,
    ILogger<TransactionCategoryRepository> logger) : ITransactionCategoryRepository
{
    public async Task<IEnumerable<TransactionCategory>> GetAllAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Fetching all transaction categories");

        return await context.TransactionCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<TransactionCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching transaction category {Id}", id);

        return await context.TransactionCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}