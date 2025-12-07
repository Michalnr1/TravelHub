using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedDeleteBehaviour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightInfos_Trips_TripId",
                table: "FlightInfos");

            migrationBuilder.AddForeignKey(
                name: "FK_FlightInfos_Trips_TripId",
                table: "FlightInfos",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlightInfos_Trips_TripId",
                table: "FlightInfos");

            migrationBuilder.AddForeignKey(
                name: "FK_FlightInfos_Trips_TripId",
                table: "FlightInfos",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id");
        }
    }
}
