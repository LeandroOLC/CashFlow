using CashFlow.Transactions.API.Domain.Entities;
using CashFlow.Transactions.API.Domain.Interfaces;
using CashFlow.Transactions.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CashFlow.Transactions.Tests.Services;

public class TransactionCategoryServiceTests
{
    private readonly Mock<ITransactionCategoryRepository> _repositoryMock = new();
    private readonly Mock<ILogger<TransactionCategoryService>> _loggerMock = new();
    private readonly TransactionCategoryService _sut;

    public TransactionCategoryServiceTests()
    {
        _sut = new TransactionCategoryService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedDtos()
    {
        var categories = new List<TransactionCategory>
        {
            TransactionCategory.Create("Salário",    "Receita mensal"),
            TransactionCategory.Create("Aluguel",    "Despesa fixa"),
            TransactionCategory.Create("Freelance",  null),
        };
        _repositoryMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(categories);

        var result = (await _sut.GetAllAsync()).ToList();

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(c => c.IsActive.Should().BeTrue());
        result.Select(c => c.Name).Should().BeEquivalentTo("Salário", "Aluguel", "Freelance");
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ShouldReturnEmptyList()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<TransactionCategory>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnDto()
    {
        var category = TransactionCategory.Create("Alimentação", "Supermercado e restaurantes");
        _repositoryMock.Setup(r => r.GetByIdAsync(category.Id, default)).ReturnsAsync(category);

        var result = await _sut.GetByIdAsync(category.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.Name.Should().Be("Alimentação");
        result.Description.Should().Be("Supermercado e restaurantes");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((TransactionCategory?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}