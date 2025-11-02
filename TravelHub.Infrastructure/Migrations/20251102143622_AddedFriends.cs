using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedFriends : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonFriends_AspNetUsers_FriendId",
                table: "PersonFriends");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonFriends_AspNetUsers_UserId",
                table: "PersonFriends");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PersonFriends",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "FriendRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AddresseeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FriendRequests_AspNetUsers_AddresseeId",
                        column: x => x.AddresseeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FriendRequests_AspNetUsers_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_AddresseeId_Status",
                table: "FriendRequests",
                columns: new[] { "AddresseeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_RequestedAt",
                table: "FriendRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_RequesterId_Status",
                table: "FriendRequests",
                columns: new[] { "RequesterId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFriends_AspNetUsers_FriendId",
                table: "PersonFriends",
                column: "FriendId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFriends_AspNetUsers_UserId",
                table: "PersonFriends",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonFriends_AspNetUsers_FriendId",
                table: "PersonFriends");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonFriends_AspNetUsers_UserId",
                table: "PersonFriends");

            migrationBuilder.DropTable(
                name: "FriendRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PersonFriends");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFriends_AspNetUsers_FriendId",
                table: "PersonFriends",
                column: "FriendId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonFriends_AspNetUsers_UserId",
                table: "PersonFriends",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
