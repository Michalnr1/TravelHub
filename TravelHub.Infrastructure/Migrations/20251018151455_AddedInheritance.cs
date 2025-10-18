using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Spots_SpotId",
                table: "Photos");

            migrationBuilder.DropForeignKey(
                name: "FK_Transports_Spots_FromSpotId",
                table: "Transports");

            migrationBuilder.DropForeignKey(
                name: "FK_Transports_Spots_ToSpotId",
                table: "Transports");

            migrationBuilder.DropTable(
                name: "Accommodations");

            migrationBuilder.DropTable(
                name: "Spots");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckIn",
                table: "Activities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckInTime",
                table: "Activities",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOut",
                table: "Activities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckOutTime",
                table: "Activities",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Activities",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Activities",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Activities",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Activities",
                type: "float",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Activities_SpotId",
                table: "Photos",
                column: "SpotId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transports_Activities_FromSpotId",
                table: "Transports",
                column: "FromSpotId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transports_Activities_ToSpotId",
                table: "Transports",
                column: "ToSpotId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_Activities_SpotId",
                table: "Photos");

            migrationBuilder.DropForeignKey(
                name: "FK_Transports_Activities_FromSpotId",
                table: "Transports");

            migrationBuilder.DropForeignKey(
                name: "FK_Transports_Activities_ToSpotId",
                table: "Transports");

            migrationBuilder.DropColumn(
                name: "CheckIn",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CheckOut",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Activities");

            migrationBuilder.CreateTable(
                name: "Spots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Spots_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accommodations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckInTime = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutTime = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accommodations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accommodations_Spots_Id",
                        column: x => x.Id,
                        principalTable: "Spots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Spots_ActivityId",
                table: "Spots",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_Spots_SpotId",
                table: "Photos",
                column: "SpotId",
                principalTable: "Spots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transports_Spots_FromSpotId",
                table: "Transports",
                column: "FromSpotId",
                principalTable: "Spots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transports_Spots_ToSpotId",
                table: "Transports",
                column: "ToSpotId",
                principalTable: "Spots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
