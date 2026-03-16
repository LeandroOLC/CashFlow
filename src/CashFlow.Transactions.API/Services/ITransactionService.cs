using CashFlow.Transactions.API.Domain.Entities;
using CashFlow.Shared.Models;

namespace CashFlow.Transactions.API.Services;

public record CreateTransactionDto(decimal Amount, string Type, DateTime Date, string Description, Guid? CategoryId);
public record UpdateTransactionDto(decimal Amount, string Type, DateTime Date, string Description, Guid? CategoryId);
public record TransactionDto(Guid Id, decimal Amount, string Type, DateTime Date, string Description, Guid? CategoryId, DateTime CreatedAt, string CreatedBy);

public interface ITransactionService
{
    Task<TransactionDto> CreateAsync(CreateTransactionDto dto, string userId, CancellationToken ct = default);
    Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<TransactionDto>> GetPagedAsync(int page, int pageSize, DateTime? startDate, DateTime? endDate, string? type, CancellationToken ct = default);
    Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
