using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutFroebel.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEcoleModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Ecoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 25, 17, 26, 57, 499, DateTimeKind.Utc).AddTicks(2689));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Ecoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 25, 17, 9, 10, 201, DateTimeKind.Utc).AddTicks(4129));
        }
    }
}
