using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedCountryFromTripToSpot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripCountries");

            migrationBuilder.CreateTable(
                name: "SpotCountries",
                columns: table => new
                {
                    SpotsId = table.Column<int>(type: "int", nullable: false),
                    CountriesCode = table.Column<string>(type: "nvarchar(3)", nullable: false),
                    CountriesName = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotCountries", x => new { x.SpotsId, x.CountriesCode, x.CountriesName });
                    table.ForeignKey(
                        name: "FK_SpotCountries_Activities_SpotsId",
                        column: x => x.SpotsId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpotCountries_Countries_CountriesCode_CountriesName",
                        columns: x => new { x.CountriesCode, x.CountriesName },
                        principalTable: "Countries",
                        principalColumns: new[] { "Code", "Name" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpotCountries_CountriesCode_CountriesName",
                table: "SpotCountries",
                columns: new[] { "CountriesCode", "CountriesName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpotCountries");

            migrationBuilder.CreateTable(
                name: "TripCountries",
                columns: table => new
                {
                    TripsId = table.Column<int>(type: "int", nullable: false),
                    CountriesCode = table.Column<string>(type: "nvarchar(3)", nullable: false),
                    CountriesName = table.Column<string>(type: "nvarchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripCountries", x => new { x.TripsId, x.CountriesCode, x.CountriesName });
                    table.ForeignKey(
                        name: "FK_TripCountries_Countries_CountriesCode_CountriesName",
                        columns: x => new { x.CountriesCode, x.CountriesName },
                        principalTable: "Countries",
                        principalColumns: new[] { "Code", "Name" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripCountries_Trips_TripsId",
                        column: x => x.TripsId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TripCountries_CountriesCode_CountriesName",
                table: "TripCountries",
                columns: new[] { "CountriesCode", "CountriesName" });
        }
    }
}
