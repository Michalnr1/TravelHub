using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedTripParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_AspNetUsers_PersonId",
                table: "Trips");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Trips",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "TripParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    PersonId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripParticipants_AspNetUsers_PersonId",
                        column: x => x.PersonId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripParticipants_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_EndDate",
                table: "Trips",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_IsPrivate",
                table: "Trips",
                column: "IsPrivate");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_StartDate",
                table: "Trips",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status",
                table: "Trips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TripParticipants_JoinedAt",
                table: "TripParticipants",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TripParticipants_PersonId",
                table: "TripParticipants",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TripParticipants_Status",
                table: "TripParticipants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TripParticipants_TripId_PersonId",
                table: "TripParticipants",
                columns: new[] { "TripId", "PersonId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_AspNetUsers_PersonId",
                table: "Trips",
                column: "PersonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_AspNetUsers_PersonId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "TripParticipants");

            migrationBuilder.DropIndex(
                name: "IX_Trips_EndDate",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_IsPrivate",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_StartDate",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_Status",
                table: "Trips");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Trips",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_AspNetUsers_PersonId",
                table: "Trips",
                column: "PersonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
