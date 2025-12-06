using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedFlightsToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlightInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    OriginAirportCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DestinationAirportCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Airline = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FlightNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BookingReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Segments = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightInfos_AspNetUsers_PersonId",
                        column: x => x.PersonId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlightInfos_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_ArrivalTime",
                table: "FlightInfos",
                column: "ArrivalTime");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_DepartureTime",
                table: "FlightInfos",
                column: "DepartureTime");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_DestinationAirportCode",
                table: "FlightInfos",
                column: "DestinationAirportCode");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_IsConfirmed",
                table: "FlightInfos",
                column: "IsConfirmed");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_OriginAirportCode",
                table: "FlightInfos",
                column: "OriginAirportCode");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_PersonId",
                table: "FlightInfos",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_TripId",
                table: "FlightInfos",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightInfos_TripId_DepartureTime",
                table: "FlightInfos",
                columns: new[] { "TripId", "DepartureTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightInfos");
        }
    }
}
