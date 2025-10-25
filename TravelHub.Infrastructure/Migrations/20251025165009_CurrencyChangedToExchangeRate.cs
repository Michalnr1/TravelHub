using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CurrencyChangedToExchangeRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Currencies_CurrencyKey",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CurrencyKey",
                table: "Expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "CurrencyKey",
                table: "Expenses");

            migrationBuilder.RenameColumn(
                name: "ExchangeRate",
                table: "Currencies",
                newName: "ExchangeRateValue");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "Currencies",
                newName: "CurrencyCodeKey");

            migrationBuilder.AddColumn<int>(
                name: "ExchangeRateId",
                table: "Expenses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Currencies",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "Currencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExchangeRateId",
                table: "Expenses",
                column: "ExchangeRateId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_TripId_CurrencyCodeKey_ExchangeRateValue",
                table: "Currencies",
                columns: new[] { "TripId", "CurrencyCodeKey", "ExchangeRateValue" },
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Currencies_Trips_TripId",
                table: "Currencies");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Currencies_ExchangeRateId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ExchangeRateId",
                table: "Expenses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies");

            migrationBuilder.DropIndex(
                name: "IX_Currencies_TripId_CurrencyCodeKey_ExchangeRateValue",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "ExchangeRateId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Currencies");

            migrationBuilder.RenameColumn(
                name: "ExchangeRateValue",
                table: "Currencies",
                newName: "ExchangeRate");

            migrationBuilder.RenameColumn(
                name: "CurrencyCodeKey",
                table: "Currencies",
                newName: "Key");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyKey",
                table: "Expenses",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CurrencyKey",
                table: "Expenses",
                column: "CurrencyKey");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Currencies_CurrencyKey",
                table: "Expenses",
                column: "CurrencyKey",
                principalTable: "Currencies",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
