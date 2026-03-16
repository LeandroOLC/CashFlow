using CashFlow.Transactions.API.Domain.Entities;

namespace CashFlow.Transactions.API.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByDateAsync(DateTime date, CancellationToken ct = default);
    Task<(IEnumerable<Transaction> Items, int Total)> GetPagedAsync(
        int page, int pageSize, DateTime? startDate, DateTime? endDate,
        TransactionType? type, CancellationToken ct = default);
}
