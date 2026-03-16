using CashFlow.Consolidation.API.Domain.Entities;
using CashFlow.Consolidation.API.Domain.Interfaces;
using CashFlow.Consolidation.API.Services;
using CashFlow.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CashFlow.Consolidation.Tests.Services;

public class ConsolidationServiceTests : IDisposable
{
    private readonly Mock<IDailyBalanceRepository> _repo = new();
    private readonly Mock<ILogger<ConsolidationService>> _logger = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly ConsolidationService _sut;

    private static readonly DateTime Day1 = new(2025, 3, 10);
    private static readonly DateTime Day2 = new(2025, 3, 11);

    public ConsolidationServiceTests()
    {
        _sut = new ConsolidationService(_repo.Object, _cache, _logger.Object);
    }

    public void Dispose() => _cache.Dispose();

    // ═══════════════════════════════════════════════════════════════════════════
    // ProcessTransactionCreatedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Created_NewDate_ShouldAddBalanceThenUpdateAndSave()
    {
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync((DailyBalance?)null);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionCreatedAsync(Day1, 1000m, "Credit");

        _repo.Verify(r => r.AddAsync(It.Is<DailyBalance>(b =>
            b.Date == Day1 && b.TotalCredits == 1000m), default), Times.Once);
        _repo.Verify(r => r.Update(It.Is<DailyBalance>(b => b.TotalCredits == 1000m)), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Created_ExistingDate_ShouldNotAddAndShouldAccumulate()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(500m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionCreatedAsync(Day1, 300m, "Credit");

        _repo.Verify(r => r.AddAsync(It.IsAny<DailyBalance>(), default), Times.Never);
        balance.TotalCredits.Should().Be(800m);
        _repo.Verify(r => r.Update(balance), Times.Once);
    }

    [Fact]
    public async Task Created_DebitType_ShouldIncreaseTotalDebitsOnly()
    {
        var balance = DailyBalance.Create(Day1);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionCreatedAsync(Day1, 250m, "Debit");

        balance.TotalDebits.Should().Be(250m);
        balance.TotalCredits.Should().Be(0m);
        balance.FinalBalance.Should().Be(-250m);
    }

    [Fact]
    public async Task Created_ShouldInvalidateCacheAfterSave()
    {
        // Popula cache antes do evento
        var dto = new DailyBalanceDto(Guid.NewGuid(), Day1, 100m, 0m, 100m, DateTime.UtcNow);
        _cache.Set($"balance:{Day1:yyyy-MM-dd}", dto, TimeSpan.FromMinutes(1));
        _cache.Set("balance:latest", dto, TimeSpan.FromMinutes(1));

        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(100m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionCreatedAsync(Day1, 200m, "Credit");

        // Cache deve ter sido invalidado — próxima leitura vai ao banco
        _cache.TryGetValue($"balance:{Day1:yyyy-MM-dd}", out _).Should().BeFalse();
        _cache.TryGetValue("balance:latest", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("CREDIT")]
    [InlineData("debit")]
    [InlineData("DEBIT")]
    public async Task Created_TypeIsCaseInsensitive_ShouldNotThrow(string type)
    {
        _repo.Setup(r => r.GetByDateAsync(It.IsAny<DateTime>(), default)).ReturnsAsync((DailyBalance?)null);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var act = async () => await _sut.ProcessTransactionCreatedAsync(Day1, 100m, type);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Pix")]
    [InlineData("Transfer")]
    [InlineData("")]
    public async Task Created_InvalidType_ShouldThrowAndNotSave(string type)
    {
        _repo.Setup(r => r.GetByDateAsync(It.IsAny<DateTime>(), default)).ReturnsAsync((DailyBalance?)null);

        var act = async () => await _sut.ProcessTransactionCreatedAsync(Day1, 100m, type);

        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ProcessTransactionUpdatedAsync — mesmo dia
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Updated_SameDay_SameCreditType_ShouldReverseOldAndApplyNew()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(200m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(200m, "Credit", Day1, 350m, "Credit", Day1);

        balance.TotalCredits.Should().Be(350m);
        balance.TotalDebits.Should().Be(0m);
        _repo.Verify(r => r.GetByDateAsync(Day1, default), Times.Once);
        _repo.Verify(r => r.Update(balance), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Updated_SameDay_CreditToDebit_ShouldZeroCreditAndAddDebit()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(500m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(500m, "Credit", Day1, 300m, "Debit", Day1);

        balance.TotalCredits.Should().Be(0m);
        balance.TotalDebits.Should().Be(300m);
        balance.FinalBalance.Should().Be(-300m);
    }

    [Fact]
    public async Task Updated_SameDay_DebitToCredit_ShouldZeroDebitAndAddCredit()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddDebit(400m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(400m, "Debit", Day1, 600m, "Credit", Day1);

        balance.TotalDebits.Should().Be(0m);
        balance.TotalCredits.Should().Be(600m);
        balance.FinalBalance.Should().Be(600m);
    }

    [Fact]
    public async Task Updated_SameDay_ShouldInvalidateCacheOnce()
    {
        var dto = new DailyBalanceDto(Guid.NewGuid(), Day1, 200m, 0m, 200m, DateTime.UtcNow);
        _cache.Set($"balance:{Day1:yyyy-MM-dd}", dto, TimeSpan.FromMinutes(1));
        _cache.Set("balance:latest", dto, TimeSpan.FromMinutes(1));

        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(200m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(200m, "Credit", Day1, 300m, "Credit", Day1);

        _cache.TryGetValue($"balance:{Day1:yyyy-MM-dd}", out _).Should().BeFalse();
        _cache.TryGetValue("balance:latest", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Updated_SameDay_WithDifferentTimeButSameDate_ShouldTreatAsSameDay()
    {
        var time1 = new DateTime(2025, 3, 10, 9, 0, 0);
        var time2 = new DateTime(2025, 3, 10, 17, 30, 0);
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(300m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(300m, "Credit", time1, 400m, "Credit", time2);

        // Apenas 1 consulta — tratado como mesmo dia
        _repo.Verify(r => r.GetByDateAsync(It.IsAny<DateTime>(), default), Times.Once);
        balance.TotalCredits.Should().Be(400m);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ProcessTransactionUpdatedAsync — dias diferentes
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Updated_DifferentDays_ShouldUpdateBothBalances()
    {
        var oldBalance = DailyBalance.Create(Day1);
        oldBalance.AddCredit(300m);
        var newBalance = DailyBalance.Create(Day2);

        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(oldBalance);
        _repo.Setup(r => r.GetByDateAsync(Day2, default)).ReturnsAsync(newBalance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(300m, "Credit", Day1, 500m, "Debit", Day2);

        oldBalance.TotalCredits.Should().Be(0m);
        newBalance.TotalDebits.Should().Be(500m);

        _repo.Verify(r => r.GetByDateAsync(Day1, default), Times.Once);
        _repo.Verify(r => r.GetByDateAsync(Day2, default), Times.Once);
        _repo.Verify(r => r.Update(oldBalance), Times.Once);
        _repo.Verify(r => r.Update(newBalance), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Updated_DifferentDays_NewDayNotExists_ShouldCreateNewBalance()
    {
        var oldBalance = DailyBalance.Create(Day1);
        oldBalance.AddDebit(100m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(oldBalance);
        _repo.Setup(r => r.GetByDateAsync(Day2, default)).ReturnsAsync((DailyBalance?)null);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(100m, "Debit", Day1, 200m, "Credit", Day2);

        _repo.Verify(r => r.AddAsync(It.Is<DailyBalance>(b =>
            b.Date == Day2 && b.TotalCredits == 200m), default), Times.Once);
        oldBalance.TotalDebits.Should().Be(0m);
    }

    [Fact]
    public async Task Updated_DifferentDays_BothNotExist_ShouldCreateBothWithoutThrowing()
    {
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync((DailyBalance?)null);
        _repo.Setup(r => r.GetByDateAsync(Day2, default)).ReturnsAsync((DailyBalance?)null);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var act = async () =>
            await _sut.ProcessTransactionUpdatedAsync(100m, "Credit", Day1, 200m, "Credit", Day2);

        await act.Should().NotThrowAsync();
        _repo.Verify(r => r.AddAsync(It.IsAny<DailyBalance>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task Updated_DifferentDays_ShouldInvalidateBothCacheKeys()
    {
        var dto1 = new DailyBalanceDto(Guid.NewGuid(), Day1, 300m, 0m, 300m, DateTime.UtcNow);
        var dto2 = new DailyBalanceDto(Guid.NewGuid(), Day2, 0m, 0m, 0m, DateTime.UtcNow);
        _cache.Set($"balance:{Day1:yyyy-MM-dd}", dto1, TimeSpan.FromMinutes(1));
        _cache.Set($"balance:{Day2:yyyy-MM-dd}", dto2, TimeSpan.FromMinutes(1));
        _cache.Set("balance:latest", dto1, TimeSpan.FromMinutes(1));

        var oldBalance = DailyBalance.Create(Day1);
        oldBalance.AddCredit(300m);
        var newBalance = DailyBalance.Create(Day2);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(oldBalance);
        _repo.Setup(r => r.GetByDateAsync(Day2, default)).ReturnsAsync(newBalance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionUpdatedAsync(300m, "Credit", Day1, 500m, "Debit", Day2);

        _cache.TryGetValue($"balance:{Day1:yyyy-MM-dd}", out _).Should().BeFalse();
        _cache.TryGetValue($"balance:{Day2:yyyy-MM-dd}", out _).Should().BeFalse();
        _cache.TryGetValue("balance:latest", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("Transfer", "Credit")]
    [InlineData("Credit", "Transfer")]
    [InlineData("", "Debit")]
    public async Task Updated_InvalidType_ShouldThrowAndNotSave(string oldType, string newType)
    {
        var balance = DailyBalance.Create(Day1);
        _repo.Setup(r => r.GetByDateAsync(It.IsAny<DateTime>(), default)).ReturnsAsync(balance);

        var act = async () =>
            await _sut.ProcessTransactionUpdatedAsync(100m, oldType, Day1, 100m, newType, Day1);

        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ProcessTransactionDeletedAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Deleted_ExistingCredit_ShouldReverseCreditOnly()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(600m);
        balance.AddDebit(100m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionDeletedAsync(Day1, 600m, "Credit");

        balance.TotalCredits.Should().Be(0m);
        balance.TotalDebits.Should().Be(100m);   // não alterado
        balance.FinalBalance.Should().Be(-100m);
        _repo.Verify(r => r.Update(balance), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Deleted_ExistingDebit_ShouldReverseDebitOnly()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(1000m);
        balance.AddDebit(400m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionDeletedAsync(Day1, 400m, "Debit");

        balance.TotalDebits.Should().Be(0m);
        balance.TotalCredits.Should().Be(1000m);  // não alterado
    }

    [Fact]
    public async Task Deleted_BalanceNotExists_ShouldIgnoreGracefullyWithoutSaving()
    {
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync((DailyBalance?)null);

        var act = async () => await _sut.ProcessTransactionDeletedAsync(Day1, 100m, "Credit");

        await act.Should().NotThrowAsync();
        _repo.Verify(r => r.Update(It.IsAny<DailyBalance>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Deleted_ShouldInvalidateCacheAfterSave()
    {
        var dto = new DailyBalanceDto(Guid.NewGuid(), Day1, 600m, 0m, 600m, DateTime.UtcNow);
        _cache.Set($"balance:{Day1:yyyy-MM-dd}", dto, TimeSpan.FromMinutes(1));
        _cache.Set("balance:latest", dto, TimeSpan.FromMinutes(1));

        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(600m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.ProcessTransactionDeletedAsync(Day1, 600m, "Credit");

        _cache.TryGetValue($"balance:{Day1:yyyy-MM-dd}", out _).Should().BeFalse();
        _cache.TryGetValue("balance:latest", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("CREDIT")]
    [InlineData("debit")]
    [InlineData("DEBIT")]
    public async Task Deleted_TypeIsCaseInsensitive_ShouldNotThrow(string type)
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(200m);
        balance.AddDebit(200m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);
        _repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var act = async () => await _sut.ProcessTransactionDeletedAsync(Day1, 100m, type);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Pix")]
    [InlineData("cash")]
    [InlineData("")]
    public async Task Deleted_InvalidType_ShouldThrowAndNotSave(string type)
    {
        var balance = DailyBalance.Create(Day1);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);

        var act = async () => await _sut.ProcessTransactionDeletedAsync(Day1, 100m, type);

        await act.Should().ThrowAsync<ArgumentException>();
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByDateAsync — com cache
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByDate_CacheMiss_ShouldHitRepoAndPopulateCache()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(1500m);
        balance.AddDebit(300m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);

        var result = await _sut.GetByDateAsync(Day1);

        result.Should().NotBeNull();
        result!.TotalCredits.Should().Be(1500m);
        result.TotalDebits.Should().Be(300m);
        result.FinalBalance.Should().Be(1200m);
        _repo.Verify(r => r.GetByDateAsync(Day1, default), Times.Once);

        // Cache foi populado — segunda chamada não vai ao banco
        await _sut.GetByDateAsync(Day1);
        _repo.Verify(r => r.GetByDateAsync(Day1, default), Times.Once);  // ainda 1
    }

    [Fact]
    public async Task GetByDate_CacheHit_ShouldNotHitRepo()
    {
        var dto = new DailyBalanceDto(Guid.NewGuid(), Day1, 800m, 200m, 600m, DateTime.UtcNow);
        _cache.Set($"balance:{Day1:yyyy-MM-dd}", dto, TimeSpan.FromMinutes(1));

        var result = await _sut.GetByDateAsync(Day1);

        result.Should().NotBeNull();
        result!.TotalCredits.Should().Be(800m);
        _repo.Verify(r => r.GetByDateAsync(It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task GetByDate_NotFound_ShouldReturnNullAndNotCache()
    {
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync((DailyBalance?)null);

        var result = await _sut.GetByDateAsync(Day1);

        result.Should().BeNull();
        _cache.TryGetValue($"balance:{Day1:yyyy-MM-dd}", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetByDate_DtoFieldsMappedCorrectly()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(500m);
        balance.AddDebit(150m);
        _repo.Setup(r => r.GetByDateAsync(Day1, default)).ReturnsAsync(balance);

        var result = await _sut.GetByDateAsync(Day1);

        result!.Id.Should().Be(balance.Id);
        result.Date.Should().Be(Day1);
        result.TotalCredits.Should().Be(500m);
        result.TotalDebits.Should().Be(150m);
        result.FinalBalance.Should().Be(350m);
        result.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetLatestAsync — com cache
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetLatest_CacheMiss_ShouldHitRepoAndPopulateCache()
    {
        var balance = DailyBalance.Create(Day2);
        balance.AddCredit(5000m);
        _repo.Setup(r => r.GetLatestAsync(default)).ReturnsAsync(balance);

        var result = await _sut.GetLatestAsync();

        result.Should().NotBeNull();
        result!.TotalCredits.Should().Be(5000m);
        _repo.Verify(r => r.GetLatestAsync(default), Times.Once);

        // Segunda chamada usa cache
        await _sut.GetLatestAsync();
        _repo.Verify(r => r.GetLatestAsync(default), Times.Once);  // ainda 1
    }

    [Fact]
    public async Task GetLatest_CacheHit_ShouldNotHitRepo()
    {
        var dto = new DailyBalanceDto(Guid.NewGuid(), Day2, 5000m, 0m, 5000m, DateTime.UtcNow);
        _cache.Set("balance:latest", dto, TimeSpan.FromMinutes(1));

        var result = await _sut.GetLatestAsync();

        result!.TotalCredits.Should().Be(5000m);
        _repo.Verify(r => r.GetLatestAsync(default), Times.Never);
    }

    [Fact]
    public async Task GetLatest_NoData_ShouldReturnNullAndNotCache()
    {
        _repo.Setup(r => r.GetLatestAsync(default)).ReturnsAsync((DailyBalance?)null);

        var result = await _sut.GetLatestAsync();

        result.Should().BeNull();
        _cache.TryGetValue("balance:latest", out _).Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByPeriodAsync — com paginação
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByPeriod_ShouldReturnMappedPagedResponse()
    {
        var balances = new List<DailyBalance>
        {
            DailyBalance.Create(Day1),
            DailyBalance.Create(Day2),
        };
        balances[0].AddCredit(100m);
        balances[1].AddDebit(50m);

        _repo.Setup(r => r.GetByPeriodPagedAsync(Day1, Day2, 1, 10, default))
             .ReturnsAsync(((IEnumerable<DailyBalance>)balances, 2));

        var result = await _sut.GetByPeriodAsync(Day1, Day2, 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetByPeriod_MultiplePages_ShouldComputeTotalPagesCorrectly()
    {
        _repo.Setup(r => r.GetByPeriodPagedAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 10, default))
             .ReturnsAsync((Enumerable.Empty<DailyBalance>(), 25));

        var result = await _sut.GetByPeriodAsync(Day1, Day2, 1, 10);

        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetByPeriod_EmptyRange_ShouldReturnEmptyPagedResponse()
    {
        _repo.Setup(r => r.GetByPeriodPagedAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1, 31, default))
             .ReturnsAsync((Enumerable.Empty<DailyBalance>(), 0));

        var result = await _sut.GetByPeriodAsync(Day1, Day2, 1, 31);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByPeriod_DtoFieldsMappedCorrectly()
    {
        var balance = DailyBalance.Create(Day1);
        balance.AddCredit(800m);
        balance.AddDebit(200m);

        _repo.Setup(r => r.GetByPeriodPagedAsync(Day1, Day1, 1, 1, default))
             .ReturnsAsync((new List<DailyBalance> { balance }, 1));

        var result = await _sut.GetByPeriodAsync(Day1, Day1, 1, 1);
        var item = result.Items.Single();

        item.Id.Should().Be(balance.Id);
        item.Date.Should().Be(Day1);
        item.TotalCredits.Should().Be(800m);
        item.TotalDebits.Should().Be(200m);
        item.FinalBalance.Should().Be(600m);
    }

    [Fact]
    public async Task GetByPeriod_ShouldPassPaginationParamsToRepository()
    {
        _repo.Setup(r => r.GetByPeriodPagedAsync(Day1, Day2, 3, 7, default))
             .ReturnsAsync((Enumerable.Empty<DailyBalance>(), 0));

        await _sut.GetByPeriodAsync(Day1, Day2, page: 3, pageSize: 7);

        _repo.Verify(r => r.GetByPeriodPagedAsync(Day1, Day2, 3, 7, default), Times.Once);
    }
}