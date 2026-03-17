using System.Net.Http.Headers;
using System.Net.Http.Json;
using CashFlow.WebApp.Models;

namespace CashFlow.WebApp.Services;

public interface ITransactionService
{
    Task<PagedResponse<TransactionDto>?> GetPagedAsync(int page, int pageSize,
        DateTime? start = null, DateTime? end = null, string? type = null);
    Task<(bool Success, string? Error)> CreateAsync(CreateTransactionRequest request);
    Task<(bool Success, string? Error)> UpdateAsync(Guid id, UpdateTransactionRequest request);
    Task<(bool Success, string? Error)> DeleteAsync(Guid id);
    Task<IEnumerable<TransactionCategoryDto>> GetCategoriesAsync();
}

public class TransactionService(HttpClient http, IAuthService auth) : ITransactionService
{
    private async Task AuthAsync()
    {
        var token = await auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<PagedResponse<TransactionDto>?> GetPagedAsync(
        int page, int pageSize,
        DateTime? start = null, DateTime? end = null, string? type = null)
    {
        try
        {
            await AuthAsync();
            var url = $"api/v1/transactions?page={page}&pageSize={pageSize}";
            if (start.HasValue) url += $"&startDate={start:yyyy-MM-dd}";
            if (end.HasValue)   url += $"&endDate={end:yyyy-MM-dd}";
            if (!string.IsNullOrEmpty(type) && type != "All") url += $"&type={type}";
            var r = await http.GetFromJsonAsync<ApiResponse<PagedResponse<TransactionDto>>>(url);
            return r?.Data;
        }
        catch ( Exception ex ){ return null; }
    }

    public async Task<IEnumerable<TransactionCategoryDto>> GetCategoriesAsync()
    {
        try
        {
            await AuthAsync();
            var r = await http.GetFromJsonAsync<ApiResponse<IEnumerable<TransactionCategoryDto>>>(
                "api/v1/transaction-categories");
            return r?.Data ?? [];
        }
        catch { return []; }
    }

    public async Task<(bool Success, string? Error)> CreateAsync(CreateTransactionRequest req)
    {
        try
        {
            await AuthAsync();
            var resp = await http.PostAsJsonAsync("api/v1/transactions", req);
            if (resp.IsSuccessStatusCode) return (true, null);
            var r = await resp.Content.ReadFromJsonAsync<ApiResponse<string>>();
            return (false, r?.Message ?? "Erro ao criar.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Guid id, UpdateTransactionRequest req)
    {
        try
        {
            await AuthAsync();
            var resp = await http.PutAsJsonAsync($"api/v1/transactions/{id}", req);
            if (resp.IsSuccessStatusCode) return (true, null);
            var r = await resp.Content.ReadFromJsonAsync<ApiResponse<string>>();
            return (false, r?.Message ?? "Erro ao atualizar.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id)
    {
        try
        {
            await AuthAsync();
            var resp = await http.DeleteAsync($"api/v1/transactions/{id}");
            return resp.IsSuccessStatusCode ? (true, null) : (false, "Erro ao excluir.");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }
}
