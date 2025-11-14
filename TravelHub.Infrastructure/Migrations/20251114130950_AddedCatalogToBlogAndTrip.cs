using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedCatalogToBlogAndTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Catalog",
                table: "Trips",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Catalog",
                table: "Blogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Catalog",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Catalog",
                table: "Blogs");
        }
    }
}
