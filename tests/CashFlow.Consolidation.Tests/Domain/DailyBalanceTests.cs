using CashFlow.Consolidation.API.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CashFlow.Consolidation.Tests.Domain;

public class DailyBalanceTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidDate_ShouldInitializeWithZeroBalances()
    {
        var balance = DailyBalance.Create(DateTime.Today);

        balance.Id.Should().NotBeEmpty();
        balance.TotalCredits.Should().Be(0m);
        balance.TotalDebits.Should().Be(0m);
        balance.FinalBalance.Should().Be(0m);
        balance.Date.Should().Be(DateTime.Today.Date);
        balance.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldNormalizeDateToMidnight()
    {
        var dateWithTime = new DateTime(2025, 3, 15, 23, 59, 59);

        var balance = DailyBalance.Create(dateWithTime);

        balance.Date.TimeOfDay.Should().Be(TimeSpan.Zero);
        balance.Date.Should().Be(new DateTime(2025, 3, 15));
    }

    // ── AddCredit ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddCredit_SingleAmount_ShouldIncreaseTotalCredits()
    {
        var balance = DailyBalance.Create(DateTime.Today);

        balance.AddCredit(1000m);

        balance.TotalCredits.Should().Be(1000m);
        balance.FinalBalance.Should().Be(1000m);
        balance.TotalDebits.Should().Be(0m);
    }

    [Fact]
    public void AddCredit_MultipleAmounts_ShouldAccumulate()
    {
        var balance = DailyBalance.Create(DateTime.Today);

        balance.AddCredit(500m);
        balance.AddCredit(300m);
        balance.AddCredit(200m);

        balance.TotalCredits.Should().Be(1000m);
    }

    [Fact]
    public void AddCredit_ShouldUpdateLastUpdated()
    {
        var balance = DailyBalance.Create(DateTime.Today);
        var beforeCall = DateTime.UtcNow;

        balance.AddCredit(100m);

        balance.LastUpdated.Should().BeOnOrAfter(beforeCall);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-500)]
    public void AddCredit_ZeroOrNegativeAmount_ShouldThrowArgumentException(decimal amount)
    {
        var balance = DailyBalance.Create(DateTime.Today);

        var act = () => balance.AddCredit(amount);

        act.Should().Throw<ArgumentException>();
    }

    // ── AddDebit ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddDebit_SingleAmount_ShouldIncreaseTotalDebits()
    {
        var balance = DailyBalance.Create(DateTime.Today);

        balance.AddDebit(400m);

        balance.TotalDebits.Should().Be(400m);
        balance.FinalBalance.Should().Be(-400m);
    }

    [Fact]
    public void AddDebit_MultipleAmounts_ShouldAccumulate()
    {
        var balance = DailyBalance.Create(DateTime.Today);

        balance.AddDebit(100m);
        balance.AddDebit(150m);
        balance.AddDebit(250m);

        balance.TotalDebits.Should().Be(500m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void AddDebit_ZeroOrNegativeAmount_ShouldThrowArgumentException(decimal amount)
    {
        var balance = DailyBalance.Create(DateTime.Today);

        var act = () => balance.AddDebit(amount);

        act.Should().Throw<ArgumentException>();
    }

    // ── FinalBalance ──────────────────────────────────────────────────────────

    [Fact]
    public void FinalBalance_CreditsGreaterThanDebits_ShouldBePositive()
    {
        var balance = DailyBalance.Create(DateTime.Today);
        balance.AddCredit(1000m);
        balance.AddDebit(600m);

        balance.FinalBalance.Should().Be(400m);
    }

    [Fact]
    public void FinalBalance_DebitsGreaterThanCredits_ShouldBeNegative()
    {
        var balance = DailyBalance.Create(DateTime.Today);
        balance.AddCredit(200m);
        balance.AddDebit(500m);

        balance.FinalBalance.Should().Be(-300m);
    }

    [Fact]
    public void FinalBalance_EqualCreditAndDebit_ShouldBeZero()
    {
        var balance = DailyBalance.Create(DateTime.Today);
        balance.AddCredit(750m);
        balance.AddDebit(750m);

        balance.FinalBalance.Should().Be(0m);
    }

    // ── Recalculate ───────────────────────────────────────────────────────────

    [Fact]
    public void Recalculate_ShouldOverwritePreviousValues()
    {
        var balance = DailyBalance.Create(DateTime.Today);
        balance.AddCredit(100m);
        balance.AddDebit(50m);

        balance.Recalculate(2000m, 800m);

        balance.TotalCredits.Should().Be(2000m);
        balance.TotalDebits.Should().Be(800m);
        balance.FinalBalance.Should().Be(1200m);
    }

    [Fact]
    public void Recalculate_WithZeroValues_ShouldResetBalance()
    {
        var balance = DailyBalance.Create(DateTime.Today);
        balance.AddCredit(500m);

        balance.Recalculate(0m, 0m);

        balance.TotalCredits.Should().Be(0m);
        balance.TotalDebits.Should().Be(0m);
        balance.FinalBalance.Should().Be(0m);
    }
}