using CashFlow.Transactions.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Transactions.API.Data.Configurations;

public class TransactionCategoryConfiguration : IEntityTypeConfiguration<TransactionCategory>
{
    // IDs fixos para seed — nunca alterar, pois são referenciados em migrations
    public static readonly Guid SalarioId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid FreelanceId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid InvestimentoId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid OutrasReceitasId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid AlimentacaoId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public static readonly Guid TransporteId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    public static readonly Guid MoradiaId = Guid.Parse("10000000-0000-0000-0000-000000000007");
    public static readonly Guid SaudeId = Guid.Parse("10000000-0000-0000-0000-000000000008");
    public static readonly Guid EducacaoId = Guid.Parse("10000000-0000-0000-0000-000000000009");
    public static readonly Guid LazerId = Guid.Parse("10000000-0000-0000-0000-000000000010");
    public static readonly Guid FornecedoresId = Guid.Parse("10000000-0000-0000-0000-000000000011");
    public static readonly Guid ImpostosId = Guid.Parse("10000000-0000-0000-0000-000000000012");
    public static readonly Guid OutrasDespesasId = Guid.Parse("10000000-0000-0000-0000-000000000013");

    public void Configure(EntityTypeBuilder<TransactionCategory> builder)
    {
        builder.ToTable("TransactionCategories", "transactions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(300).IsRequired(false);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("IX_TransactionCategories_Name");

        // Seed: Receitas (Credit)
        SeedCategory(builder, SalarioId, "Salário", "Salário mensal e benefícios");
        SeedCategory(builder, FreelanceId, "Freelance", "Receitas de trabalhos autônomos e prestação de serviços");
        SeedCategory(builder, InvestimentoId, "Investimentos", "Rendimentos de aplicações, dividendos e renda variável");
        SeedCategory(builder, OutrasReceitasId, "Outras Receitas", "Receitas diversas não classificadas");

        // Seed: Despesas (Debit)
        SeedCategory(builder, AlimentacaoId, "Alimentação", "Supermercado, restaurantes, delivery e refeições");
        SeedCategory(builder, TransporteId, "Transporte", "Combustível, aplicativos, transporte público e manutenção veicular");
        SeedCategory(builder, MoradiaId, "Moradia", "Aluguel, condomínio, IPTU, água, luz e gás");
        SeedCategory(builder, SaudeId, "Saúde", "Plano de saúde, consultas, exames e medicamentos");
        SeedCategory(builder, EducacaoId, "Educação", "Mensalidades, cursos, livros e treinamentos");
        SeedCategory(builder, LazerId, "Lazer", "Entretenimento, viagens, streaming e assinaturas");
        SeedCategory(builder, FornecedoresId, "Fornecedores", "Pagamentos a fornecedores e parceiros comerciais");
        SeedCategory(builder, ImpostosId, "Impostos e Taxas", "DAS, DARF, ISS, IOF e demais obrigações fiscais");
        SeedCategory(builder, OutrasDespesasId, "Outras Despesas", "Despesas diversas não classificadas");
    }

    private static void SeedCategory(EntityTypeBuilder<TransactionCategory> builder,
        Guid id, string name, string description)
    {
        builder.HasData(new
        {
            Id = id,
            Name = name,
            Description = description,
            IsActive = true
        });
    }
}