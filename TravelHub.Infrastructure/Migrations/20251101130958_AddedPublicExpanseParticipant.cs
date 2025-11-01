using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedPublicExpanseParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseParticipants_AspNetUsers_ParticipantsId",
                table: "ExpenseParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseParticipants_Expenses_ExpensesToCoverId",
                table: "ExpenseParticipants");

            migrationBuilder.RenameColumn(
                name: "ParticipantsId",
                table: "ExpenseParticipants",
                newName: "PersonId");

            migrationBuilder.RenameColumn(
                name: "ExpensesToCoverId",
                table: "ExpenseParticipants",
                newName: "ExpenseId");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseParticipants_ParticipantsId",
                table: "ExpenseParticipants",
                newName: "IX_ExpenseParticipants_PersonId");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualShareValue",
                table: "ExpenseParticipants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Share",
                table: "ExpenseParticipants",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseParticipants_AspNetUsers_PersonId",
                table: "ExpenseParticipants",
                column: "PersonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseParticipants_Expenses_ExpenseId",
                table: "ExpenseParticipants",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseParticipants_AspNetUsers_PersonId",
                table: "ExpenseParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseParticipants_Expenses_ExpenseId",
                table: "ExpenseParticipants");

            migrationBuilder.DropColumn(
                name: "ActualShareValue",
                table: "ExpenseParticipants");

            migrationBuilder.DropColumn(
                name: "Share",
                table: "ExpenseParticipants");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                table: "ExpenseParticipants",
                newName: "ParticipantsId");

            migrationBuilder.RenameColumn(
                name: "ExpenseId",
                table: "ExpenseParticipants",
                newName: "ExpensesToCoverId");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseParticipants_PersonId",
                table: "ExpenseParticipants",
                newName: "IX_ExpenseParticipants_ParticipantsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseParticipants_AspNetUsers_ParticipantsId",
                table: "ExpenseParticipants",
                column: "ParticipantsId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseParticipants_Expenses_ExpensesToCoverId",
                table: "ExpenseParticipants",
                column: "ExpensesToCoverId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
