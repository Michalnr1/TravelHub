using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AppDbContextChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Currencies_Trips_TripId",
                table: "Currencies");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Currencies_ExchangeRateId",
                table: "Expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies");

            migrationBuilder.RenameTable(
                name: "Currencies",
                newName: "ExchangeRates");

            migrationBuilder.RenameIndex(
                name: "IX_Currencies_TripId_CurrencyCodeKey_ExchangeRateValue",
                table: "ExchangeRates",
                newName: "IX_ExchangeRates_TripId_CurrencyCodeKey_ExchangeRateValue");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExchangeRates",
                table: "ExchangeRates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeRates_Trips_TripId",
                table: "ExchangeRates",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_ExchangeRates_ExchangeRateId",
                table: "Expenses",
                column: "ExchangeRateId",
                principalTable: "ExchangeRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeRates_Trips_TripId",
                table: "ExchangeRates");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_ExchangeRates_ExchangeRateId",
                table: "Expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExchangeRates",
                table: "ExchangeRates");

            migrationBuilder.RenameTable(
                name: "ExchangeRates",
                newName: "Currencies");

            migrationBuilder.RenameIndex(
                name: "IX_ExchangeRates_TripId_CurrencyCodeKey_ExchangeRateValue",
                table: "Currencies",
                newName: "IX_Currencies_TripId_CurrencyCodeKey_ExchangeRateValue");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Currencies_Trips_TripId",
                table: "Currencies",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Currencies_ExchangeRateId",
                table: "Expenses",
                column: "ExchangeRateId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
