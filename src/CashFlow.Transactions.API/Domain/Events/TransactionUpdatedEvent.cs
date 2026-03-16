namespace CashFlow.Transactions.API.Domain.Events;

/// <summary>
/// Publicado quando um lançamento é alterado.
/// Carrega tanto os valores anteriores quanto os novos para que o
/// Consolidation Service possa recalcular o saldo do(s) dia(s) afetado(s).
/// </summary>
public record TransactionUpdatedEvent(
    Guid TransactionId,
    // Valores ANTERIORES à edição
    decimal OldAmount,
    string OldType,
    DateTime OldDate,
    // Valores NOVOS após a edição
    decimal NewAmount,
    string NewType,
    DateTime NewDate,
    DateTime OccurredAt);
