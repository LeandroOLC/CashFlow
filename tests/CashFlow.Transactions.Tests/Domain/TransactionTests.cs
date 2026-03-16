using CashFlow.Transactions.API.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CashFlow.Transactions.Tests.Domain;

public class TransactionTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ShouldReturnPopulatedTransaction()
    {
        var amount = 100.50m;
        var type = TransactionType.Credit;
        var date = DateTime.Today;
        var description = "Salário mensal";
        var userId = "user-123";

        var transaction = Transaction.Create(amount, type, date, description, userId);

        transaction.Id.Should().NotBeEmpty();
        transaction.Amount.Should().Be(amount);
        transaction.Type.Should().Be(type);
        transaction.Date.Should().Be(date.Date);
        transaction.Description.Should().Be(description);
        transaction.CreatedBy.Should().Be(userId);
        transaction.CategoryId.Should().BeNull();
        transaction.UpdatedAt.Should().BeNull();
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithCategory_ShouldSetCategoryId()
    {
        var categoryId = Guid.NewGuid();

        var transaction = Transaction.Create(500m, TransactionType.Debit, DateTime.Today, "Aluguel", "user", categoryId);

        transaction.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public void Create_ShouldNormalizeDateToMidnight()
    {
        var dateWithTime = new DateTime(2025, 3, 15, 14, 30, 59);

        var transaction = Transaction.Create(100m, TransactionType.Credit, dateWithTime, "Test", "user");

        transaction.Date.Should().Be(new DateTime(2025, 3, 15));
        transaction.Date.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Create_TwoTransactions_ShouldHaveDifferentIds()
    {
        var t1 = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "T1", "user");
        var t2 = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "T2", "user");

        t1.Id.Should().NotBe(t2.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-999)]
    public void Create_ZeroOrNegativeAmount_ShouldThrowArgumentException(decimal amount)
    {
        var act = () => Transaction.Create(amount, TransactionType.Credit, DateTime.Today, "Test", "user");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNullDescription_ShouldThrowArgumentException(string? description)
    {
        var act = () => Transaction.Create(100m, TransactionType.Credit, DateTime.Today, description!, "user");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("description");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ValidData_ShouldChangeFieldsAndSetUpdatedAt()
    {
        var transaction = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "Original", "user");

        transaction.Update(250m, TransactionType.Debit, DateTime.Today.AddDays(-1), "Atualizado");

        transaction.Amount.Should().Be(250m);
        transaction.Type.Should().Be(TransactionType.Debit);
        transaction.Description.Should().Be("Atualizado");
        transaction.UpdatedAt.Should().NotBeNull();
        transaction.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_WithNewCategory_ShouldUpdateCategoryId()
    {
        var categoryId = Guid.NewGuid();
        var transaction = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "Test", "user");

        transaction.Update(100m, TransactionType.Credit, DateTime.Today, "Test", categoryId);

        transaction.CategoryId.Should().Be(categoryId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_InvalidAmount_ShouldThrowArgumentException(decimal amount)
    {
        var transaction = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "Test", "user");

        var act = () => transaction.Update(amount, TransactionType.Credit, DateTime.Today, "Test");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Update_EmptyDescription_ShouldThrowArgumentException(string? description)
    {
        var transaction = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "Test", "user");

        var act = () => transaction.Update(100m, TransactionType.Credit, DateTime.Today, description!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("description");
    }

    [Fact]
    public void Update_ShouldNotChangeCreatedAtOrCreatedBy()
    {
        var transaction = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "Test", "user-original");
        var originalCreatedAt = transaction.CreatedAt;

        transaction.Update(200m, TransactionType.Debit, DateTime.Today, "Changed");

        transaction.CreatedAt.Should().Be(originalCreatedAt);
        transaction.CreatedBy.Should().Be("user-original");
    }
}