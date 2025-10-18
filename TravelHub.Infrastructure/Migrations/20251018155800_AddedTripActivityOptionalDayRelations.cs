using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedTripActivityOptionalDayRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Days_DayId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Days_Trips_TripId",
                table: "Days");

            migrationBuilder.AlterColumn<int>(
                name: "DayId",
                table: "Activities",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Activities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TripId",
                table: "Activities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CategoryId",
                table: "Activities",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_TripId",
                table: "Activities",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Categories_CategoryId",
                table: "Activities",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Days_DayId",
                table: "Activities",
                column: "DayId",
                principalTable: "Days",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Trips_TripId",
                table: "Activities",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Days_Trips_TripId",
                table: "Days",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Categories_CategoryId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Days_DayId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Trips_TripId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Days_Trips_TripId",
                table: "Days");

            migrationBuilder.DropIndex(
                name: "IX_Activities_CategoryId",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_TripId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "TripId",
                table: "Activities");

            migrationBuilder.AlterColumn<int>(
                name: "DayId",
                table: "Activities",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Days_DayId",
                table: "Activities",
                column: "DayId",
                principalTable: "Days",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Days_Trips_TripId",
                table: "Days",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
