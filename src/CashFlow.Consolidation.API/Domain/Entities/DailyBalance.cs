namespace CashFlow.Consolidation.API.Domain.Entities;

public class DailyBalance
{
    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public decimal FinalBalance => TotalCredits - TotalDebits;
    public DateTime LastUpdated { get; private set; }

    private DailyBalance() { }

    public static DailyBalance Create(DateTime date) =>
        new() { Id = Guid.NewGuid(), Date = date.Date, TotalCredits = 0, TotalDebits = 0, LastUpdated = DateTime.UtcNow };

    // ── Adição direta ─────────────────────────────────────────────────────────
    public void AddCredit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(amount));
        TotalCredits += amount;
        LastUpdated = DateTime.UtcNow;
    }

    public void AddDebit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(amount));
        TotalDebits += amount;
        LastUpdated = DateTime.UtcNow;
    }

    // ── Estorno (usado em Update e Delete) ────────────────────────────────────
    /// <summary>
    /// Reverte um crédito previamente somado.
    /// TotalCredits nunca fica negativo — indica inconsistência de dados.
    /// </summary>
    public void ReverseCredit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(amount));
        TotalCredits = Math.Max(0, TotalCredits - amount);
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Reverte um débito previamente somado.
    /// TotalDebits nunca fica negativo — indica inconsistência de dados.
    /// </summary>
    public void ReverseDebit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Valor deve ser positivo", nameof(amount));
        TotalDebits = Math.Max(0, TotalDebits - amount);
        LastUpdated = DateTime.UtcNow;
    }

    // ── Recálculo completo (para reconciliação) ───────────────────────────────
    public void Recalculate(decimal totalCredits, decimal totalDebits)
    {
        if (totalCredits < 0) throw new ArgumentException("TotalCredits não pode ser negativo");
        if (totalDebits < 0) throw new ArgumentException("TotalDebits não pode ser negativo");
        TotalCredits = totalCredits;
        TotalDebits = totalDebits;
        LastUpdated = DateTime.UtcNow;
    }
}
