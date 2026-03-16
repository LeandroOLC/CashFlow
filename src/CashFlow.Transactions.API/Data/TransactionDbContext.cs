using CashFlow.Transactions.API.Data.Configurations;
using CashFlow.Transactions.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.API.Data;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionCategoryConfiguration());
    }
}
