using CashFlow.Transactions.API.Domain.Entities;
using CashFlow.Transactions.API.Domain.Interfaces;
using CashFlow.Transactions.API.Infrastructure.Messaging;
using CashFlow.Transactions.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CashFlow.Transactions.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _repositoryMock = new();
    private readonly Mock<IRabbitMqPublisher> _publisherMock = new();
    private readonly Mock<ILogger<TransactionService>> _loggerMock = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _sut = new TransactionService(_repositoryMock.Object, _publisherMock.Object, _loggerMock.Object);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidCreditDto_ShouldPersistAndPublishEvent()
    {
        var dto = new CreateTransactionDto(1500m, "Credit", DateTime.Today, "Freelance", null);
        _repositoryMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(dto, "user-abc");

        result.Should().NotBeNull();
        result.Amount.Should().Be(1500m);
        result.Type.Should().Be("Credit");
        result.CreatedBy.Should().Be("user-abc");
        result.Id.Should().NotBeEmpty();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
        _publisherMock.Verify(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidDebitDto_ShouldReturnDebitType()
    {
        var dto = new CreateTransactionDto(300m, "Debit", DateTime.Today, "Supermercado", null);
        _repositoryMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(dto, "user-abc");

        result.Type.Should().Be("Debit");
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("CREDIT")]
    [InlineData("debit")]
    [InlineData("DEBIT")]
    public async Task CreateAsync_TypeIsCaseInsensitive_ShouldAccept(string type)
    {
        var dto = new CreateTransactionDto(100m, type, DateTime.Today, "Test", null);
        _repositoryMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var act = async () => await _sut.CreateAsync(dto, "user");

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Transferencia")]
    [InlineData("cash")]
    [InlineData("")]
    public async Task CreateAsync_InvalidType_ShouldThrowArgumentException(string type)
    {
        var dto = new CreateTransactionDto(100m, type, DateTime.Today, "Test", null);

        var act = async () => await _sut.CreateAsync(dto, "user");

        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Transaction>(), default), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPublisherThrows_ShouldStillReturnResult()
    {
        // Resiliência: falha no RabbitMQ não deve derrubar o lançamento
        var dto = new CreateTransactionDto(100m, "Credit", DateTime.Today, "Test", null);
        _repositoryMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);
        _publisherMock.Setup(p => p.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("RabbitMQ unavailable"));

        var result = await _sut.CreateAsync(dto, "user");

        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnMappedDto()
    {
        var transaction = Transaction.Create(200m, TransactionType.Debit, DateTime.Today, "Aluguel", "user");
        _repositoryMock.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);

        var result = await _sut.GetByIdAsync(transaction.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(transaction.Id);
        result.Amount.Should().Be(200m);
        result.Type.Should().Be("Debit");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Transaction?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResponse()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "T1", "user"),
            Transaction.Create(200m, TransactionType.Debit,  DateTime.Today, "T2", "user"),
        };
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 10, null, null, null, default))
            .ReturnsAsync((transactions, 2));

        var result = await _sut.GetPagedAsync(1, 10, null, null, null);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_InvalidType_ShouldIgnoreTypeFilter()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 10, null, null, null, default))
            .ReturnsAsync((new List<Transaction>(), 0));

        // Tipo inválido deve ser ignorado (não lançar exception), filtrando sem tipo
        var act = async () => await _sut.GetPagedAsync(1, 10, null, null, "Invalido");

        await act.Should().NotThrowAsync();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingId_ShouldUpdateAndReturnDto()
    {
        var transaction = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "Original", "user");
        _repositoryMock.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);
        _repositoryMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new UpdateTransactionDto(999m, "Debit", DateTime.Today, "Atualizado", null);
        var result = await _sut.UpdateAsync(transaction.Id, dto);

        result.Amount.Should().Be(999m);
        result.Type.Should().Be("Debit");
        result.Description.Should().Be("Atualizado");
        _repositoryMock.Verify(r => r.Update(transaction), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Transaction?)null);

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(),
            new UpdateTransactionDto(100m, "Credit", DateTime.Today, "Test", null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repositoryMock.Verify(r => r.Update(It.IsAny<Transaction>()), Times.Never);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ShouldRemoveAndSave()
    {
        var transaction = Transaction.Create(100m, TransactionType.Credit, DateTime.Today, "Test", "user");
        _repositoryMock.Setup(r => r.GetByIdAsync(transaction.Id, default)).ReturnsAsync(transaction);
        _repositoryMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _sut.DeleteAsync(transaction.Id);

        _repositoryMock.Verify(r => r.Remove(transaction), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Transaction?)null);

        var act = async () => await _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repositoryMock.Verify(r => r.Remove(It.IsAny<Transaction>()), Times.Never);
    }
}