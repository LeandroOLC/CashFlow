namespace CashFlow.WebApp.Models;

// ── Auth ──────────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string FullName, string Password, string ConfirmPassword);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, string UserId, string Email);

// ── Transactions ──────────────────────────────────────────────────────────────
public record TransactionDto(
    Guid Id, decimal Amount, string Type, DateTime Date,
    string Description, Guid? CategoryId, DateTime CreatedAt, string CreatedBy);

public record CreateTransactionRequest(decimal Amount, string Type, DateTime Date, string Description, Guid? CategoryId);
public record UpdateTransactionRequest(decimal Amount, string Type, DateTime Date, string Description, Guid? CategoryId);
public record TransactionCategoryDto(Guid Id, string Name, string? Description, bool IsActive);

// ── Consolidation ─────────────────────────────────────────────────────────────
public record DailyBalanceDto(
    Guid Id, DateTime Date, decimal TotalCredits,
    decimal TotalDebits, decimal FinalBalance, DateTime LastUpdated);

// ── Shared ────────────────────────────────────────────────────────────────────
public class ApiResponse<T>
{
    public bool    Success  { get; set; }
    public T?      Data     { get; set; }
    public string? Message  { get; set; }
    public IEnumerable<string>? Errors { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items      { get; set; } = [];
    public int            TotalCount { get; set; }
    public int            Page       { get; set; }
    public int            PageSize   { get; set; }
    public int            TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
