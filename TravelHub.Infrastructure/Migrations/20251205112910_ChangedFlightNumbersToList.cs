using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedFlightNumbersToList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlightNumber",
                table: "FlightInfos");

            migrationBuilder.AddColumn<string>(
                name: "FlightNumbers",
                table: "FlightInfos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_BookingReference",
                table: "FlightInfos",
                column: "BookingReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlightInfos_BookingReference",
                table: "FlightInfos");

            migrationBuilder.DropColumn(
                name: "FlightNumbers",
                table: "FlightInfos");

            migrationBuilder.AddColumn<string>(
                name: "FlightNumber",
                table: "FlightInfos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
