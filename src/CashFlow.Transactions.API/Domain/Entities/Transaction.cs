namespace CashFlow.Transactions.API.Domain.Entities;

public enum TransactionType { Credit, Debit }

public class Transaction
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? CategoryId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private Transaction() { }

    public static Transaction Create(decimal amount, TransactionType type, DateTime date,
        string description, string createdBy, Guid? categoryId = null)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required", nameof(description));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Type = type,
            Date = date.Date,
            Description = description,
            CategoryId = categoryId,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(decimal amount, TransactionType type, DateTime date, string description, Guid? categoryId = null)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required", nameof(description));

        Amount = amount;
        Type = type;
        Date = date.Date;
        Description = description;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }
}
