using System.Net.Http.Headers;
using System.Net.Http.Json;
using CashFlow.BlazorWasm.Models;

namespace CashFlow.BlazorWasm.Services;

public interface IConsolidationService
{
    Task<DailyBalanceDto?> GetLatestAsync();
    Task<DailyBalanceDto?> GetByDateAsync(DateTime date);
    Task<PagedResponse<DailyBalanceDto>?> GetByPeriodAsync(
        DateTime start, DateTime end, int page = 1, int pageSize = 31);
}

public class ConsolidationService(
    IHttpClientFactory factory,
    IAuthService auth) : IConsolidationService
{
    private async Task<HttpClient> GetHttpAsync()
    {
        var http = factory.CreateClient("consolidation");
        var token = await auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    public async Task<DailyBalanceDto?> GetLatestAsync()
    {
        var http = await GetHttpAsync();
        try
        {
            var r = await http.GetFromJsonAsync<ApiResponse<DailyBalanceDto>>(
                "/api/v1/daily-consolidation/latest");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<DailyBalanceDto?> GetByDateAsync(DateTime date)
    {
        var http = await GetHttpAsync();
        try
        {
            var r = await http.GetFromJsonAsync<ApiResponse<DailyBalanceDto>>(
                $"/api/v1/daily-consolidation/{date:yyyy-MM-dd}");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<PagedResponse<DailyBalanceDto>?> GetByPeriodAsync(
        DateTime start, DateTime end, int page = 1, int pageSize = 31)
    {
        var http = await GetHttpAsync();
        try
        {
            var url = $"/api/v1/daily-consolidation/period" +
                      $"?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}" +
                      $"&page={page}&pageSize={pageSize}";
            var r = await http.GetFromJsonAsync<ApiResponse<PagedResponse<DailyBalanceDto>>>(url);
            return r?.Data;
        }
        catch { return null; }
    }
}