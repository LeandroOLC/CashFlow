using CashFlow.Transactions.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Transactions.API.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", "transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(t => t.Date).HasColumnType("date").IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired(false);
        builder.Property(t => t.CategoryId).IsRequired(false);

        builder.HasIndex(t => t.Date).HasDatabaseName("IX_Transactions_Date");
        builder.HasIndex(t => t.Type).HasDatabaseName("IX_Transactions_Type");
        builder.HasIndex(t => new { t.Date, t.Type }).HasDatabaseName("IX_Transactions_Date_Type");

        builder.HasOne<TransactionCategory>()
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // ── Seed de transações padrão ─────────────────────────────────────────
        // Datas fixas (EF Core exige valores constantes em HasData)
        // Cobre os últimos 30 dias a partir de 2025-03-01 (referência estável)
        var seededAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            // ── Receitas ──────────────────────────────────────────────────────
            Seed("20000000-0000-0000-0000-000000000001", 8_500.00m, "Credit", 2026, 3, 1, "Salário março", "10000000-0000-0000-0000-000000000001", seededAt),
            Seed("20000000-0000-0000-0000-000000000002", 1_200.00m, "Credit", 2026, 3, 5, "Freelance - projeto site institucional", "10000000-0000-0000-0000-000000000002", seededAt),
            Seed("20000000-0000-0000-0000-000000000003", 350.75m, "Credit", 2026, 3, 8, "Dividendos FII MXRF11", "10000000-0000-0000-0000-000000000003", seededAt),
            Seed("20000000-0000-0000-0000-000000000004", 500.00m, "Credit", 2026, 3, 12, "Consultoria mensal - cliente ABC", "10000000-0000-0000-0000-000000000002", seededAt),
            Seed("20000000-0000-0000-0000-000000000005", 189.90m, "Credit", 2026, 3, 15, "Cashback cartão de crédito", "10000000-0000-0000-0000-000000000004", seededAt),
            Seed("20000000-0000-0000-0000-000000000006", 8_500.00m, "Credit", 2026, 2, 1, "Salário fevereiro", "10000000-0000-0000-0000-000000000001", seededAt),
            Seed("20000000-0000-0000-0000-000000000007", 750.00m, "Credit", 2026, 2, 10, "Freelance - identidade visual", "10000000-0000-0000-0000-000000000002", seededAt),
            Seed("20000000-0000-0000-0000-000000000008", 412.30m, "Credit", 2026, 2, 14, "Rendimento CDB 90 dias", "10000000-0000-0000-0000-000000000003", seededAt),

            // ── Despesas ──────────────────────────────────────────────────────
            Seed("20000000-0000-0000-0000-000000000009", 2_100.00m, "Debit", 2026, 3, 1, "Aluguel março", "10000000-0000-0000-0000-000000000007", seededAt),
            Seed("20000000-0000-0000-0000-000000000010", 480.50m, "Debit", 2026, 3, 3, "Supermercado quinzenal", "10000000-0000-0000-0000-000000000005", seededAt),
            Seed("20000000-0000-0000-0000-000000000011", 220.00m, "Debit", 2026, 3, 4, "Combustível mensal", "10000000-0000-0000-0000-000000000006", seededAt),
            Seed("20000000-0000-0000-0000-000000000012", 399.90m, "Debit", 2026, 3, 5, "Plano de saúde", "10000000-0000-0000-0000-000000000008", seededAt),
            Seed("20000000-0000-0000-0000-000000000013", 199.00m, "Debit", 2026, 3, 6, "Curso .NET Avançado - Udemy", "10000000-0000-0000-0000-000000000009", seededAt),
            Seed("20000000-0000-0000-0000-000000000014", 89.90m, "Debit", 2026, 3, 7, "Netflix + Spotify", "10000000-0000-0000-0000-000000000010", seededAt),
            Seed("20000000-0000-0000-0000-000000000015", 312.00m, "Debit", 2026, 3, 10, "Conta de luz e água", "10000000-0000-0000-0000-000000000007", seededAt),
            Seed("20000000-0000-0000-0000-000000000016", 1_450.00m, "Debit", 2026, 3, 12, "Nota fiscal fornecedor TechParts", "10000000-0000-0000-0000-000000000011", seededAt),
            Seed("20000000-0000-0000-0000-000000000017", 560.00m, "Debit", 2026, 3, 13, "DAS Simples Nacional março", "10000000-0000-0000-0000-000000000012", seededAt),
            Seed("20000000-0000-0000-0000-000000000018", 265.40m, "Debit", 2026, 3, 15, "Supermercado quinzenal", "10000000-0000-0000-0000-000000000005", seededAt),
            Seed("20000000-0000-0000-0000-000000000019", 130.00m, "Debit", 2026, 3, 17, "Uber e transporte público", "10000000-0000-0000-0000-000000000006", seededAt),
            Seed("20000000-0000-0000-0000-000000000020", 350.00m, "Debit", 2026, 3, 20, "Jantar aniversário - restaurante", "10000000-0000-0000-0000-000000000010", seededAt),
            Seed("20000000-0000-0000-0000-000000000021", 2_100.00m, "Debit", 2026, 2, 1, "Aluguel fevereiro", "10000000-0000-0000-0000-000000000007", seededAt),
            Seed("20000000-0000-0000-0000-000000000022", 510.20m, "Debit", 2026, 2, 5, "Supermercado", "10000000-0000-0000-0000-000000000005", seededAt),
            Seed("20000000-0000-0000-0000-000000000023", 399.90m, "Debit", 2026, 2, 6, "Plano de saúde", "10000000-0000-0000-0000-000000000008", seededAt),
            Seed("20000000-0000-0000-0000-000000000024", 560.00m, "Debit", 2026, 2, 10, "DAS Simples Nacional fevereiro", "10000000-0000-0000-0000-000000000012", seededAt),
            Seed("20000000-0000-0000-0000-000000000025", 175.00m, "Debit", 2026, 2, 18, "Manutenção notebook", "10000000-0000-0000-0000-000000000013", seededAt)
        );
    }

    private static object Seed(
        string id, decimal amount, string type,
        int year, int month, int day,
        string description, string categoryId,
        DateTime seededAt) => new
        {
            Id = Guid.Parse(id),
            Amount = amount,
            Type = Enum.Parse<TransactionType>(type),
            Date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc),
            Description = description,
            CategoryId = (Guid?)Guid.Parse(categoryId),
            CreatedBy = "seed",
            CreatedAt = seededAt,
            UpdatedAt = (DateTime?)null
        };
}
