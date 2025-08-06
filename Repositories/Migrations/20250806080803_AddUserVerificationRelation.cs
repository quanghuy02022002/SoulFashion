using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVerificationRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Costumes_Users_OwnerUserId",
                table: "Costumes");

            migrationBuilder.DropIndex(
                name: "IX_Costumes_OwnerUserId",
                table: "Costumes");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Costumes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserVerifications",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "UserVerifications",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Costumes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Costumes_OwnerUserId",
                table: "Costumes",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Costumes_Users_OwnerUserId",
                table: "Costumes",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
