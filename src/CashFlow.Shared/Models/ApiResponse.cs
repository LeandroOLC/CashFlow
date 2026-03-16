namespace CashFlow.Shared.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public string? CorrelationId { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, string? correlationId = null) =>
        new() { Success = true, Data = data, Message = message, CorrelationId = correlationId };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null, string? correlationId = null) =>
        new() { Success = false, Message = message, Errors = errors, CorrelationId = correlationId };
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
