using CashFlow.Transactions.API.Domain.Interfaces;

namespace CashFlow.Transactions.API.Services;

public class TransactionCategoryService(
    ITransactionCategoryRepository repository,
    ILogger<TransactionCategoryService> logger) : ITransactionCategoryService
{
    public async Task<IEnumerable<TransactionCategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Listing all transaction categories");

        var categories = await repository.GetAllAsync(ct);
        return categories.Select(MapToDto);
    }

    public async Task<TransactionCategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching transaction category {Id}", id);

        var category = await repository.GetByIdAsync(id, ct);
        return category is null ? null : MapToDto(category);
    }

    private static TransactionCategoryDto MapToDto(Domain.Entities.TransactionCategory c) =>
        new(c.Id, c.Name, c.Description, c.IsActive);
}