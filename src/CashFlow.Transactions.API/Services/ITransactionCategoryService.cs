namespace CashFlow.Transactions.API.Services;

public record TransactionCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

public interface ITransactionCategoryService
{
    Task<IEnumerable<TransactionCategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<TransactionCategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}