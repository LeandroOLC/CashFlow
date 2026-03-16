using CashFlow.Transactions.API.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CashFlow.Transactions.Tests.Domain;

public class TransactionCategoryTests
{
    [Fact]
    public void Create_ValidData_ShouldReturnActiveCategory()
    {
        var category = TransactionCategory.Create("Salário", "Receita mensal");

        category.Id.Should().NotBeEmpty();
        category.Name.Should().Be("Salário");
        category.Description.Should().Be("Receita mensal");
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutDescription_ShouldHaveNullDescription()
    {
        var category = TransactionCategory.Create("Outros");

        category.Description.Should().BeNull();
        category.IsActive.Should().BeTrue();
    }
}