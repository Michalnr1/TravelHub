using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedAccommodationToDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccommodationId",
                table: "Days",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Days_AccommodationId",
                table: "Days",
                column: "AccommodationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Days_Activities_AccommodationId",
                table: "Days",
                column: "AccommodationId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Days_Activities_AccommodationId",
                table: "Days");

            migrationBuilder.DropIndex(
                name: "IX_Days_AccommodationId",
                table: "Days");

            migrationBuilder.DropColumn(
                name: "AccommodationId",
                table: "Days");
        }
    }
}
