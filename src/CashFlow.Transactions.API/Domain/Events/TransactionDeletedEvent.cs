namespace CashFlow.Transactions.API.Domain.Events;

/// <summary>
/// Publicado quando um lançamento é removido.
/// O Consolidation Service usa Amount e Type para estornar o valor do saldo diário.
/// </summary>
public record TransactionDeletedEvent(
    Guid TransactionId,
    decimal Amount,
    string Type,
    DateTime Date,
    DateTime OccurredAt);
