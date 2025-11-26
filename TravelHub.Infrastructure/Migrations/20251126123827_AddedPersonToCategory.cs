using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedPersonToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Categories_CategoryId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Categories_CategoryId",
                table: "Expenses");

            migrationBuilder.AddColumn<string>(
                name: "PersonId",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE c 
                SET c.PersonId = (
                    SELECT TOP 1 u.Id 
                    FROM AspNetUsers u 
                    ORDER BY u.Id
                )
                FROM Categories c
                WHERE c.PersonId IS NULL
            ");

            migrationBuilder.AlterColumn<string>(
                name: "PersonId",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_PersonId",
                table: "Categories",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Categories_CategoryId",
                table: "Activities",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_PersonId",
                table: "Categories",
                column: "PersonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Categories_CategoryId",
                table: "Expenses",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Categories_CategoryId",
                table: "Activities");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_PersonId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Categories_CategoryId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Categories_PersonId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Categories");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Categories_CategoryId",
                table: "Activities",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Categories_CategoryId",
                table: "Expenses",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
