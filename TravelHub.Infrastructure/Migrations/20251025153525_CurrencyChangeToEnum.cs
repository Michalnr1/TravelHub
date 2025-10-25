using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CurrencyChangeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Currencies_CurrencyKey",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Currencies");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Currencies_CurrencyKey",
                table: "Expenses",
                column: "CurrencyKey",
                principalTable: "Currencies",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Currencies_CurrencyKey",
                table: "Expenses");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Currencies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Currencies_CurrencyKey",
                table: "Expenses",
                column: "CurrencyKey",
                principalTable: "Currencies",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
