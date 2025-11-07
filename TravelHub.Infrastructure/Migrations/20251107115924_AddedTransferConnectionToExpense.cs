using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedTransferConnectionToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferredToId",
                table: "Expenses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TransferredToId",
                table: "Expenses",
                column: "TransferredToId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_AspNetUsers_TransferredToId",
                table: "Expenses",
                column: "TransferredToId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_AspNetUsers_TransferredToId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TransferredToId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TransferredToId",
                table: "Expenses");
        }
    }
}
