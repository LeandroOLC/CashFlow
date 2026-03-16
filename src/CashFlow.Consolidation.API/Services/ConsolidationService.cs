using CashFlow.Consolidation.API.Domain.Entities;
using CashFlow.Consolidation.API.Domain.Interfaces;
using CashFlow.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CashFlow.Consolidation.API.Services;

public class ConsolidationService(
    IDailyBalanceRepository repository,
    IMemoryCache cache,
    ILogger<ConsolidationService> logger) : IConsolidationService
{
    // Cache key por data: "balance:2025-03-15"
    private static string CacheKey(DateTime date) => $"balance:{date:yyyy-MM-dd}";
    private const string LatestKey = "balance:latest";

    // ── Evento: Transaction CRIADA ────────────────────────────────────────────
    public async Task ProcessTransactionCreatedAsync(
        DateTime date, decimal amount, string type, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Created: {Type} {Amount} on {Date}", type, amount, date.Date);
        var balance = await GetOrCreateBalanceAsync(date, ct);
        ApplyTransaction(balance, type, amount);
        repository.Update(balance);
        await repository.SaveChangesAsync(ct);
        InvalidateCache(date);
        LogBalance("Created", balance);
    }

    // ── Evento: Transaction ATUALIZADA ────────────────────────────────────────
    public async Task ProcessTransactionUpdatedAsync(
        decimal oldAmount, string oldType, DateTime oldDate,
        decimal newAmount, string newType, DateTime newDate,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Processing Updated: {OldType} {OldAmount} on {OldDate} → {NewType} {NewAmount} on {NewDate}",
            oldType, oldAmount, oldDate.Date, newType, newAmount, newDate.Date);

        if (oldDate.Date == newDate.Date)
        {
            var balance = await GetOrCreateBalanceAsync(oldDate, ct);
            ReverseTransaction(balance, oldType, oldAmount);
            ApplyTransaction(balance, newType, newAmount);
            repository.Update(balance);
            await repository.SaveChangesAsync(ct);
            InvalidateCache(oldDate);
        }
        else
        {
            var oldBalance = await GetOrCreateBalanceAsync(oldDate, ct);
            ReverseTransaction(oldBalance, oldType, oldAmount);
            repository.Update(oldBalance);

            var newBalance = await GetOrCreateBalanceAsync(newDate, ct);
            ApplyTransaction(newBalance, newType, newAmount);
            repository.Update(newBalance);

            await repository.SaveChangesAsync(ct);
            InvalidateCache(oldDate);
            InvalidateCache(newDate);
        }
    }

    // ── Evento: Transaction DELETADA ──────────────────────────────────────────
    public async Task ProcessTransactionDeletedAsync(
        DateTime date, decimal amount, string type, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Deleted: {Type} {Amount} on {Date}", type, amount, date.Date);
        var balance = await repository.GetByDateAsync(date, ct);
        if (balance is null)
        {
            logger.LogWarning("DailyBalance não encontrado para {Date}. Ignorado.", date.Date);
            return;
        }
        ReverseTransaction(balance, type, amount);
        repository.Update(balance);
        await repository.SaveChangesAsync(ct);
        InvalidateCache(date);
        LogBalance("Deleted", balance);
    }

    // ── Consultas com cache ───────────────────────────────────────────────────
    public async Task<DailyBalanceDto?> GetByDateAsync(DateTime date, CancellationToken ct = default)
    {
        var key = CacheKey(date);
        if (cache.TryGetValue(key, out DailyBalanceDto? cached))
        {
            logger.LogDebug("Cache HIT for {Date}", date.Date);
            return cached;
        }

        var balance = await repository.GetByDateAsync(date, ct);
        if (balance is null) return null;

        var dto = MapToDto(balance);
        // TTL de 30s — balances são atualizados pelo consumer assíncrono
        cache.Set(key, dto, TimeSpan.FromSeconds(30));
        return dto;
    }

    public async Task<DailyBalanceDto?> GetLatestAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(LatestKey, out DailyBalanceDto? cached))
            return cached;

        var balance = await repository.GetLatestAsync(ct);
        if (balance is null) return null;

        var dto = MapToDto(balance);
        cache.Set(LatestKey, dto, TimeSpan.FromSeconds(30));
        return dto;
    }

    public async Task<PagedResponse<DailyBalanceDto>> GetByPeriodAsync(
        DateTime startDate, DateTime endDate,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var (items, total) = await repository.GetByPeriodPagedAsync(startDate, endDate, page, pageSize, ct);

        return new PagedResponse<DailyBalanceDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<DailyBalance> GetOrCreateBalanceAsync(DateTime date, CancellationToken ct)
    {
        var balance = await repository.GetByDateAsync(date, ct);
        if (balance is not null) return balance;

        balance = DailyBalance.Create(date);
        await repository.AddAsync(balance, ct);
        return balance;
    }

    private static void ApplyTransaction(DailyBalance balance, string type, decimal amount)
    {
        if (type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            balance.AddCredit(amount);
        else if (type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
            balance.AddDebit(amount);
        else
            throw new ArgumentException($"Tipo desconhecido: {type}");
    }

    private static void ReverseTransaction(DailyBalance balance, string type, decimal amount)
    {
        if (type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            balance.ReverseCredit(amount);
        else if (type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
            balance.ReverseDebit(amount);
        else
            throw new ArgumentException($"Tipo desconhecido: {type}");
    }

    private void InvalidateCache(DateTime date)
    {
        cache.Remove(CacheKey(date));
        cache.Remove(LatestKey);
        logger.LogDebug("Cache invalidado para {Date}", date.Date);
    }

    private void LogBalance(string op, DailyBalance b) =>
        logger.LogInformation(
            "[{Op}] DailyBalance {Date}: Credits={Credits} Debits={Debits} Final={Final}",
            op, b.Date.ToString("yyyy-MM-dd"), b.TotalCredits, b.TotalDebits, b.FinalBalance);

    private static DailyBalanceDto MapToDto(DailyBalance b) =>
        new(b.Id, b.Date, b.TotalCredits, b.TotalDebits, b.FinalBalance, b.LastUpdated);
}