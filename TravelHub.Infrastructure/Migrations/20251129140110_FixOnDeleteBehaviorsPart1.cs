using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOnDeleteBehaviorsPart1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Activities_SpotId",
                table: "Expenses");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Activities_SpotId",
                table: "Expenses",
                column: "SpotId",
                principalTable: "Activities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Activities_SpotId",
                table: "Expenses");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Activities_SpotId",
                table: "Expenses",
                column: "SpotId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
