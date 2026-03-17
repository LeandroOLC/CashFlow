using System.Net.Http.Headers;
using System.Net.Http.Json;
using CashFlow.WebApp.Models;

namespace CashFlow.WebApp.Services;

public interface IConsolidationService
{
    Task<DailyBalanceDto?> GetLatestAsync();
    Task<PagedResponse<DailyBalanceDto>?> GetByPeriodAsync(
        DateTime start, DateTime end, int page = 1, int pageSize = 31);
}

public class ConsolidationService(HttpClient http, IAuthService auth) : IConsolidationService
{
    private async Task AuthAsync()
    {
        var token = await auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<DailyBalanceDto?> GetLatestAsync()
    {
        try
        {
            await AuthAsync();
            var r = await http.GetFromJsonAsync<ApiResponse<DailyBalanceDto>>(
                "api/v1/daily-consolidation/latest");
            return r?.Data;
        }
        catch { return null; }
    }

    public async Task<PagedResponse<DailyBalanceDto>?> GetByPeriodAsync(
        DateTime start, DateTime end, int page = 1, int pageSize = 31)
    {
        try
        {
            await AuthAsync();
            var url = $"api/v1/daily-consolidation/period" +
                      $"?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}" +
                      $"&page={page}&pageSize={pageSize}";
            var r = await http.GetFromJsonAsync<ApiResponse<PagedResponse<DailyBalanceDto>>>(url);
            return r?.Data;
        }
        catch { return null; }
    }
}
