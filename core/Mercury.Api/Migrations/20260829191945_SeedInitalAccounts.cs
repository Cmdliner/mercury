using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mercury.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitalAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "id", "code", "name", "type" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "STORE-A-PENDING", "Store A – Pending Settlement", "Asset" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "STORE-A-CASH-TILL", "Store A – Cash Till", "Asset" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "PHARMACY-BANK", "Pharmacy Bank Account", "Asset" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "SALES-REVENUE-A", "Sales Revenue – Store A", "Revenue" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "REFUNDS-A", "Refunds – Store A", "Revenue" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));
        }
    }
}
