using CashFlow.Transactions.API.Domain.Entities;
using CashFlow.Transactions.API.Domain.Events;
using CashFlow.Transactions.API.Domain.Interfaces;
using CashFlow.Transactions.API.Infrastructure.Messaging;
using CashFlow.Shared.Models;

namespace CashFlow.Transactions.API.Services;

public class TransactionService(
    ITransactionRepository repository,
    IRabbitMqPublisher publisher,
    ILogger<TransactionService> logger) : ITransactionService
{
    private const string Exchange = "cashflow.transactions";
    private const string RoutingKeyCreated = "transaction.created";
    private const string RoutingKeyUpdated = "transaction.updated";
    private const string RoutingKeyDeleted = "transaction.deleted";

    public async Task<TransactionDto> CreateAsync(CreateTransactionDto dto, string userId, CancellationToken ct = default)
    {
        logger.LogInformation("Creating transaction for user {UserId}: {Amount} {Type}", userId, dto.Amount, dto.Type);

        if (!Enum.TryParse<TransactionType>(dto.Type, true, out var type))
            throw new ArgumentException($"Tipo de lançamento inválido: {dto.Type}");

        var transaction = Transaction.Create(dto.Amount, type, dto.Date, dto.Description, userId, dto.CategoryId);

        await repository.AddAsync(transaction, ct);
        await repository.SaveChangesAsync(ct);

        await PublishSafeAsync(
            new TransactionCreatedEvent(
                transaction.Id, transaction.Amount, transaction.Type.ToString(),
                transaction.Date, transaction.Description, DateTime.UtcNow),
            Exchange, RoutingKeyCreated, ct);

        return MapToDto(transaction);
    }

    public async Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await repository.GetByIdAsync(id, ct);
        return transaction is null ? null : MapToDto(transaction);
    }

    public async Task<PagedResponse<TransactionDto>> GetPagedAsync(
        int page, int pageSize,
        DateTime? startDate, DateTime? endDate,
        string? type, CancellationToken ct = default)
    {
        TransactionType? transactionType = null;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, true, out var parsed))
            transactionType = parsed;

        var (items, total) = await repository.GetPagedAsync(page, pageSize, startDate, endDate, transactionType, ct);

        return new PagedResponse<TransactionDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionDto dto, CancellationToken ct = default)
    {
        logger.LogInformation("Updating transaction {Id}", id);

        var transaction = await repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Lançamento {id} não encontrado");

        if (!Enum.TryParse<TransactionType>(dto.Type, true, out var newType))
            throw new ArgumentException($"Tipo de lançamento inválido: {dto.Type}");

        // Captura os valores ANTES da alteração para o evento
        var oldAmount = transaction.Amount;
        var oldType = transaction.Type.ToString();
        var oldDate = transaction.Date;

        transaction.Update(dto.Amount, newType, dto.Date, dto.Description, dto.CategoryId);
        repository.Update(transaction);
        await repository.SaveChangesAsync(ct);

        // Publica com valores antigos E novos — consumer recalcula ambos os dias
        await PublishSafeAsync(
            new TransactionUpdatedEvent(
                transaction.Id,
                oldAmount, oldType, oldDate,
                transaction.Amount, transaction.Type.ToString(), transaction.Date,
                DateTime.UtcNow),
            Exchange, RoutingKeyUpdated, ct);

        return MapToDto(transaction);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Deleting transaction {Id}", id);

        var transaction = await repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Lançamento {id} não encontrado");

        // Captura ANTES de remover — precisamos dos valores para estornar o saldo
        var deletedEvent = new TransactionDeletedEvent(
            transaction.Id,
            transaction.Amount,
            transaction.Type.ToString(),
            transaction.Date,
            DateTime.UtcNow);

        repository.Remove(transaction);
        await repository.SaveChangesAsync(ct);

        // Publica APÓS salvar com sucesso — garante consistência
        await PublishSafeAsync(deletedEvent, Exchange, RoutingKeyDeleted, ct);
    }

    // Garante que uma falha no RabbitMQ não derruba a operação principal,
    // mas loga o erro para auditoria / dead-letter manual
    private async Task PublishSafeAsync<T>(T message, string exchange, string routingKey, CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(message, exchange, routingKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha ao publicar {EventType} no RabbitMQ. Exchange={Exchange} RoutingKey={RoutingKey}. " +
                "O saldo consolidado pode ficar inconsistente e precisar de reconciliação manual.",
                typeof(T).Name, exchange, routingKey);
        }
    }

    private static TransactionDto MapToDto(Transaction t) =>
        new(t.Id, t.Amount, t.Type.ToString(), t.Date, t.Description, t.CategoryId, t.CreatedAt, t.CreatedBy);
}
