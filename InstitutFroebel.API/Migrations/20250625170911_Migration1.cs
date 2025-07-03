using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InstitutFroebel.API.Migrations
{
    /// <inheritdoc />
    public partial class Migration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOuverture",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "Devise",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "FuseauHoraire",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "Langue",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "Pays",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "SiteWeb",
                table: "Ecoles");

            migrationBuilder.RenameColumn(
                name: "Ville",
                table: "Ecoles",
                newName: "Commune");

            migrationBuilder.UpdateData(
                table: "Ecoles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Adresse", "Commune", "CreatedAt", "Nom" },
                values: new object[] { "Marcory Anoumambo, en face de l'ARTCI", "Marcory", new DateTime(2025, 6, 25, 17, 9, 10, 201, DateTimeKind.Utc).AddTicks(4129), "Institut Froebel LA TULIPE" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Commune",
                table: "Ecoles",
                newName: "Ville");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOuverture",
                table: "Ecoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Devise",
                table: "Ecoles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "FCFA");

            migrationBuilder.AddColumn<string>(
                name: "FuseauHoraire",
                table: "Ecoles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Africa/Abidjan");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Ecoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Langue",
                table: "Ecoles",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "fr");

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "Ecoles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pays",
                table: "Ecoles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SiteWeb",
                table: "Ecoles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Ecoles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Adresse", "CreatedAt", "DateOuverture", "Devise", "FuseauHoraire", "IsActive", "Langue", "Logo", "Nom", "Pays", "SiteWeb", "Ville" },
                values: new object[] { "123 Rue de la Paix", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "FCFA", "Africa/Abidjan", true, "fr", null, "Institut Froebel Démonstration", "Côte d'Ivoire", null, "Abidjan" });
        }
    }
}
