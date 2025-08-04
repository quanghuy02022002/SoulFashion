using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserIdToCostume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserVerifications_UserId",
                table: "UserVerifications");

            migrationBuilder.DropIndex(
                name: "IX_ReturnInspections_OrderId",
                table: "ReturnInspections");

            migrationBuilder.DropIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Costumes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_UserId",
                table: "UserVerifications",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnInspections_OrderId",
                table: "ReturnInspections",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserVerifications_UserId",
                table: "UserVerifications");

            migrationBuilder.DropIndex(
                name: "IX_ReturnInspections_OrderId",
                table: "ReturnInspections");

            migrationBuilder.DropIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Costumes");

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_UserId",
                table: "UserVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnInspections_OrderId",
                table: "ReturnInspections",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_OrderId",
                table: "Deposits",
                column: "OrderId");
        }
    }
}
