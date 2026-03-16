namespace CashFlow.Transactions.API.Domain.Events;

public record TransactionCreatedEvent(
    Guid TransactionId,
    decimal Amount,
    string Type,
    DateTime Date,
    string Description,
    DateTime OccurredAt);
