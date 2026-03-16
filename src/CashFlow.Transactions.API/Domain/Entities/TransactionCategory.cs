namespace CashFlow.Transactions.API.Domain.Entities;

public class TransactionCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<Transaction> Transactions { get; private set; } = [];

    private TransactionCategory() { }

    public static TransactionCategory Create(string name, string? description = null) =>
        new() { Id = Guid.NewGuid(), Name = name, Description = description, IsActive = true };
}
