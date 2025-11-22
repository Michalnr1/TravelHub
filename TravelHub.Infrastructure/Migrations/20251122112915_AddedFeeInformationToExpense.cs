using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedFeeInformationToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalFee",
                table: "Expenses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentageFee",
                table: "Expenses",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalFee",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PercentageFee",
                table: "Expenses");
        }
    }
}
