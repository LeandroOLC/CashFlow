using CashFlow.Consolidation.API.Data.Configurations;
using CashFlow.Consolidation.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CashFlow.Consolidation.API.Data;

public class ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : DbContext(options)
{
    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new DailyBalanceConfiguration());
    }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    // Suprime o PendingModelChangesWarning — as migrations manuais estão sincronizadas com o snapshot
    //    optionsBuilder.ConfigureWarnings(w =>
    //        w.Ignore(RelationalEventId.PendingModelChangesWarning));
    //}
}
