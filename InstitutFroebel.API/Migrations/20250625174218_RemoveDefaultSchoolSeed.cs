using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutFroebel.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultSchoolSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Ecoles",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Ecoles",
                columns: new[] { "Id", "Adresse", "Code", "Commune", "CreatedAt", "Email", "IsDeleted", "Nom", "Telephone", "UpdatedAt" },
                values: new object[] { 1, "Marcory Anoumambo, en face de l'ARTCI", "DEMO_SCHOOL", "Marcory", new DateTime(2025, 6, 25, 17, 26, 57, 499, DateTimeKind.Utc).AddTicks(2689), "demo@froebel.edu", false, "Institut Froebel LA TULIPE", "+225 01 02 03 04 05", null });
        }
    }
}
