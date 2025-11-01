using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedSpotAndTransportConnectionToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Transports");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Activities");

            migrationBuilder.AddColumn<bool>(
                name: "IsEstimated",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SpotId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransportId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SpotId",
                table: "Expenses",
                column: "SpotId",
                unique: true,
                filter: "[SpotId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TransportId",
                table: "Expenses",
                column: "TransportId",
                unique: true,
                filter: "[TransportId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Activities_SpotId",
                table: "Expenses",
                column: "SpotId",
                principalTable: "Activities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Transports_TransportId",
                table: "Expenses",
                column: "TransportId",
                principalTable: "Transports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Activities_SpotId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Transports_TransportId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SpotId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TransportId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "IsEstimated",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SpotId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TransportId",
                table: "Expenses");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Transports",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Activities",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
