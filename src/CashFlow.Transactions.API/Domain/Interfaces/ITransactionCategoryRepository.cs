using CashFlow.Transactions.API.Domain.Entities;

namespace CashFlow.Transactions.API.Domain.Interfaces;

public interface ITransactionCategoryRepository
{
    Task<IEnumerable<TransactionCategory>> GetAllAsync(CancellationToken ct = default);
    Task<TransactionCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
}