using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedCountriesAndCurrencyToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Trips",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => new { x.Code, x.Name });
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripCountries");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Trips");
        }
    }
}
