using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCostumeForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        
       


            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "Costumes",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Costumes",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true,
                oldDefaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Costumes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Costumes_CreatedByUserId",
                table: "Costumes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Costumes_OwnerUserId",
                table: "Costumes",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Costumes_Users_CreatedByUserId",
                table: "Costumes",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Costumes_Users_OwnerUserId",
                table: "Costumes",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Costumes_Users_CreatedByUserId",
                table: "Costumes");

            migrationBuilder.DropForeignKey(
                name: "FK_Costumes_Users_OwnerUserId",
                table: "Costumes");

            migrationBuilder.DropIndex(
                name: "IX_Costumes_CreatedByUserId",
                table: "Costumes");

            migrationBuilder.DropIndex(
                name: "IX_Costumes_OwnerUserId",
                table: "Costumes");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Costumes");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "Costumes",
                type: "int",
                nullable: true,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Costumes",
                type: "bit",
                nullable: true,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Costumes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Costumes_OwnerId",
                table: "Costumes",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK__Costumes__OwnerI__48CFD27E",
                table: "Costumes",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
