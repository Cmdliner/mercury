using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercury.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "accounts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "type",
                value: "Expense");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "accounts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.UpdateData(
                table: "accounts",
                keyColumn: "id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "type",
                value: "Revenue");
        }
    }
}
