using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedSpotCountryRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpotCountries");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Activities",
                type: "nvarchar(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryName",
                table: "Activities",
                type: "nvarchar(100)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CountryCode_CountryName",
                table: "Activities",
                columns: new[] { "CountryCode", "CountryName" });

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Countries_CountryCode_CountryName",
                table: "Activities",
                columns: new[] { "CountryCode", "CountryName" },
                principalTable: "Countries",
                principalColumns: new[] { "Code", "Name" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Countries_CountryCode_CountryName",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_CountryCode_CountryName",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CountryName",
                table: "Activities");

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
    }
}
