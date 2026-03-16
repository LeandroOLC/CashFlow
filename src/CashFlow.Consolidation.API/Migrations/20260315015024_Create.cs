using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CashFlow.Consolidation.API.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyBalances", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DailyBalances",
                columns: new[] { "Id", "Date", "LastUpdated", "TotalCredits", "TotalDebits" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8500.00m, 2100.00m },
                    { new Guid("30000000-0000-0000-0000-000000000002"), new DateTime(2026, 2, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 510.20m },
                    { new Guid("30000000-0000-0000-0000-000000000003"), new DateTime(2026, 2, 6, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 399.90m },
                    { new Guid("30000000-0000-0000-0000-000000000004"), new DateTime(2026, 2, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 750.00m, 560.00m },
                    { new Guid("30000000-0000-0000-0000-000000000005"), new DateTime(2026, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 412.30m, 0.00m },
                    { new Guid("30000000-0000-0000-0000-000000000006"), new DateTime(2026, 2, 18, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 175.00m },
                    { new Guid("30000000-0000-0000-0000-000000000007"), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8500.00m, 2100.00m },
                    { new Guid("30000000-0000-0000-0000-000000000008"), new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 480.50m },
                    { new Guid("30000000-0000-0000-0000-000000000009"), new DateTime(2026, 3, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 220.00m },
                    { new Guid("30000000-0000-0000-0000-000000000010"), new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1200.00m, 399.90m },
                    { new Guid("30000000-0000-0000-0000-000000000011"), new DateTime(2026, 3, 6, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 199.00m },
                    { new Guid("30000000-0000-0000-0000-000000000012"), new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 89.90m },
                    { new Guid("30000000-0000-0000-0000-000000000013"), new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 350.75m, 0.00m },
                    { new Guid("30000000-0000-0000-0000-000000000014"), new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 312.00m },
                    { new Guid("30000000-0000-0000-0000-000000000015"), new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 500.00m, 1450.00m },
                    { new Guid("30000000-0000-0000-0000-000000000016"), new DateTime(2026, 3, 13, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 560.00m },
                    { new Guid("30000000-0000-0000-0000-000000000017"), new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 189.90m, 265.40m },
                    { new Guid("30000000-0000-0000-0000-000000000018"), new DateTime(2026, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 130.00m },
                    { new Guid("30000000-0000-0000-0000-000000000019"), new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0.00m, 350.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyBalances_Date",
                table: "DailyBalances",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyBalances");
        }
    }
}
