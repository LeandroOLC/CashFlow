using CashFlow.Consolidation.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Consolidation.API.Data.Configurations;

public class DailyBalanceConfiguration : IEntityTypeConfiguration<DailyBalance>
{
    // IDs fixos — nunca alterar após gerar a migration
    // Calculados somando exatamente as transactions seed do TransactionConfiguration
    private static readonly DateTime SeededAt = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<DailyBalance> builder)
    {
        builder.ToTable("DailyBalances");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Date).HasColumnType("date").IsRequired();
        builder.Property(d => d.TotalCredits).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.TotalDebits).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.LastUpdated).IsRequired();

        builder.Ignore(d => d.FinalBalance);

        builder.HasIndex(d => d.Date).IsUnique().HasDatabaseName("IX_DailyBalances_Date");

        // ── Seed: saldos calculados a partir das transactions default ──────────
        // Fórmula por dia: TotalCredits = Σ créditos do dia
        //                  TotalDebits  = Σ débitos do dia
        //                  FinalBalance = TotalCredits - TotalDebits (calculado)
        builder.HasData(

            // ══ Fevereiro/2026 ════════════════════════════════════════════════
            // 01/02 → Salário (8.500) - Aluguel (2.100) = +6.400
            Seed("30000000-0000-0000-0000-000000000001", 2026, 2, 1, credits: 8_500.00m, debits: 2_100.00m),

            // 05/02 → Supermercado (-510,20) = -510,20
            Seed("30000000-0000-0000-0000-000000000002", 2026, 2, 5, credits: 0.00m, debits: 510.20m),

            // 06/02 → Plano de saúde (-399,90) = -399,90
            Seed("30000000-0000-0000-0000-000000000003", 2026, 2, 6, credits: 0.00m, debits: 399.90m),

            // 10/02 → Freelance (750) - DAS (560) = +190
            Seed("30000000-0000-0000-0000-000000000004", 2026, 2, 10, credits: 750.00m, debits: 560.00m),

            // 14/02 → Rendimento CDB (412,30) = +412,30
            Seed("30000000-0000-0000-0000-000000000005", 2026, 2, 14, credits: 412.30m, debits: 0.00m),

            // 18/02 → Manutenção notebook (-175) = -175
            Seed("30000000-0000-0000-0000-000000000006", 2026, 2, 18, credits: 0.00m, debits: 175.00m),

            // ══ Março/2026 ════════════════════════════════════════════════════
            // 01/03 → Salário (8.500) - Aluguel (2.100) = +6.400
            Seed("30000000-0000-0000-0000-000000000007", 2026, 3, 1, credits: 8_500.00m, debits: 2_100.00m),

            // 03/03 → Supermercado (-480,50) = -480,50
            Seed("30000000-0000-0000-0000-000000000008", 2026, 3, 3, credits: 0.00m, debits: 480.50m),

            // 04/03 → Combustível (-220) = -220
            Seed("30000000-0000-0000-0000-000000000009", 2026, 3, 4, credits: 0.00m, debits: 220.00m),

            // 05/03 → Freelance (1.200) - Plano de saúde (399,90) = +800,10
            Seed("30000000-0000-0000-0000-000000000010", 2026, 3, 5, credits: 1_200.00m, debits: 399.90m),

            // 06/03 → Curso Udemy (-199) = -199
            Seed("30000000-0000-0000-0000-000000000011", 2026, 3, 6, credits: 0.00m, debits: 199.00m),

            // 07/03 → Netflix + Spotify (-89,90) = -89,90
            Seed("30000000-0000-0000-0000-000000000012", 2026, 3, 7, credits: 0.00m, debits: 89.90m),

            // 08/03 → Dividendos FII (350,75) = +350,75
            Seed("30000000-0000-0000-0000-000000000013", 2026, 3, 8, credits: 350.75m, debits: 0.00m),

            // 10/03 → Conta de luz e água (-312) = -312
            Seed("30000000-0000-0000-0000-000000000014", 2026, 3, 10, credits: 0.00m, debits: 312.00m),

            // 12/03 → Consultoria (500) - Fornecedor TechParts (1.450) = -950
            Seed("30000000-0000-0000-0000-000000000015", 2026, 3, 12, credits: 500.00m, debits: 1_450.00m),

            // 13/03 → DAS Simples Nacional (-560) = -560
            Seed("30000000-0000-0000-0000-000000000016", 2026, 3, 13, credits: 0.00m, debits: 560.00m),

            // 15/03 → Cashback (189,90) - Supermercado (265,40) = -75,50
            Seed("30000000-0000-0000-0000-000000000017", 2026, 3, 15, credits: 189.90m, debits: 265.40m),

            // 17/03 → Uber e transporte (-130) = -130
            Seed("30000000-0000-0000-0000-000000000018", 2026, 3, 17, credits: 0.00m, debits: 130.00m),

            // 20/03 → Jantar aniversário (-350) = -350
            Seed("30000000-0000-0000-0000-000000000019", 2026, 3, 20, credits: 0.00m, debits: 350.00m));

    }

    private static object Seed(
       string id, int year, int month, int day,
       decimal credits, decimal debits) => new
       {
           Id = Guid.Parse(id),
           Date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc),
           TotalCredits = credits,
           TotalDebits = debits,
           LastUpdated = SeededAt
       };
}
