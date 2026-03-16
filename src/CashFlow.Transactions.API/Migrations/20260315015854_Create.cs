using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CashFlow.Transactions.API.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "transactions");

            migrationBuilder.CreateTable(
                name: "TransactionCategories",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_TransactionCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "transactions",
                        principalTable: "TransactionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                schema: "transactions",
                table: "TransactionCategories",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Salário mensal e benefícios", true, "Salário" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Receitas de trabalhos autônomos e prestação de serviços", true, "Freelance" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Rendimentos de aplicações, dividendos e renda variável", true, "Investimentos" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Receitas diversas não classificadas", true, "Outras Receitas" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Supermercado, restaurantes, delivery e refeições", true, "Alimentação" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Combustível, aplicativos, transporte público e manutenção veicular", true, "Transporte" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Aluguel, condomínio, IPTU, água, luz e gás", true, "Moradia" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "Plano de saúde, consultas, exames e medicamentos", true, "Saúde" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "Mensalidades, cursos, livros e treinamentos", true, "Educação" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "Entretenimento, viagens, streaming e assinaturas", true, "Lazer" },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "Pagamentos a fornecedores e parceiros comerciais", true, "Fornecedores" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "DAS, DARF, ISS, IOF e demais obrigações fiscais", true, "Impostos e Taxas" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "Despesas diversas não classificadas", true, "Outras Despesas" }
                });

            migrationBuilder.InsertData(
                schema: "transactions",
                table: "Transactions",
                columns: new[] { "Id", "Amount", "CategoryId", "CreatedAt", "CreatedBy", "Date", "Description", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), 8500.00m, new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Salário março", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000002"), 1200.00m, new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Freelance - projeto site institucional", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000003"), 350.75m, new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Dividendos FII MXRF11", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000004"), 500.00m, new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Consultoria mensal - cliente ABC", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000005"), 189.90m, new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Cashback cartão de crédito", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000006"), 8500.00m, new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Salário fevereiro", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000007"), 750.00m, new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Freelance - identidade visual", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000008"), 412.30m, new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), "Rendimento CDB 90 dias", "Credit", null },
                    { new Guid("20000000-0000-0000-0000-000000000009"), 2100.00m, new Guid("10000000-0000-0000-0000-000000000007"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Aluguel março", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000010"), 480.50m, new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Supermercado quinzenal", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000011"), 220.00m, new Guid("10000000-0000-0000-0000-000000000006"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Combustível mensal", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000012"), 399.90m, new Guid("10000000-0000-0000-0000-000000000008"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Plano de saúde", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000013"), 199.00m, new Guid("10000000-0000-0000-0000-000000000009"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Curso .NET Avançado - Udemy", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000014"), 89.90m, new Guid("10000000-0000-0000-0000-000000000010"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), "Netflix + Spotify", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000015"), 312.00m, new Guid("10000000-0000-0000-0000-000000000007"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Conta de luz e água", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000016"), 1450.00m, new Guid("10000000-0000-0000-0000-000000000011"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Nota fiscal fornecedor TechParts", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000017"), 560.00m, new Guid("10000000-0000-0000-0000-000000000012"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 13, 0, 0, 0, 0, DateTimeKind.Utc), "DAS Simples Nacional março", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000018"), 265.40m, new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Supermercado quinzenal", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000019"), 130.00m, new Guid("10000000-0000-0000-0000-000000000006"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), "Uber e transporte público", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000020"), 350.00m, new Guid("10000000-0000-0000-0000-000000000010"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Jantar aniversário - restaurante", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000021"), 2100.00m, new Guid("10000000-0000-0000-0000-000000000007"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Aluguel fevereiro", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000022"), 510.20m, new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Supermercado", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000023"), 399.90m, new Guid("10000000-0000-0000-0000-000000000008"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Plano de saúde", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000024"), 560.00m, new Guid("10000000-0000-0000-0000-000000000012"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc), "DAS Simples Nacional fevereiro", "Debit", null },
                    { new Guid("20000000-0000-0000-0000-000000000025"), 175.00m, new Guid("10000000-0000-0000-0000-000000000013"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), "seed", new DateTime(2026, 2, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Manutenção notebook", "Debit", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionCategories_Name",
                schema: "transactions",
                table: "TransactionCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                schema: "transactions",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date",
                schema: "transactions",
                table: "Transactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date_Type",
                schema: "transactions",
                table: "Transactions",
                columns: new[] { "Date", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Type",
                schema: "transactions",
                table: "Transactions",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "transactions");

            migrationBuilder.DropTable(
                name: "TransactionCategories",
                schema: "transactions");
        }
    }
}
