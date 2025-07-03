using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InstitutFroebel.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntityRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enfants_Users_ParentId",
                table: "Enfants");

            migrationBuilder.DropTable(
                name: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Enfants_EcoleId_NumeroEtudiant",
                table: "Enfants");

            migrationBuilder.DropIndex(
                name: "IX_Enfants_ParentId",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "CantinePaye",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "NumeroEtudiant",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Enfants");

            migrationBuilder.AddColumn<string>(
                name: "AnneeScolaire",
                table: "Enfants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Cantines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateMenu = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TypeRepas = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "dejeuner"),
                    Menu = table.Column<string>(type: "text", nullable: false),
                    Prix = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Semaine = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cantines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cantines_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParentEnfants",
                columns: table => new
                {
                    ParentId = table.Column<string>(type: "text", nullable: false),
                    EnfantId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentEnfants", x => new { x.ParentId, x.EnfantId });
                    table.ForeignKey(
                        name: "FK_ParentEnfants_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentEnfants_Enfants_EnfantId",
                        column: x => x.EnfantId,
                        principalTable: "Enfants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentEnfants_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherEnfants",
                columns: table => new
                {
                    TeacherId = table.Column<string>(type: "text", nullable: false),
                    EnfantId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherEnfants", x => new { x.TeacherId, x.EnfantId });
                    table.ForeignKey(
                        name: "FK_TeacherEnfants_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherEnfants_Enfants_EnfantId",
                        column: x => x.EnfantId,
                        principalTable: "Enfants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherEnfants_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enfants_EcoleId_AnneeScolaire",
                table: "Enfants",
                columns: new[] { "EcoleId", "AnneeScolaire" });

            migrationBuilder.CreateIndex(
                name: "IX_Cantines_EcoleId",
                table: "Cantines",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cantines_EcoleId_DateMenu",
                table: "Cantines",
                columns: new[] { "EcoleId", "DateMenu" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentEnfants_EcoleId",
                table: "ParentEnfants",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentEnfants_EnfantId",
                table: "ParentEnfants",
                column: "EnfantId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentEnfants_ParentId",
                table: "ParentEnfants",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEnfants_EcoleId",
                table: "TeacherEnfants",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEnfants_EnfantId",
                table: "TeacherEnfants",
                column: "EnfantId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherEnfants_TeacherId",
                table: "TeacherEnfants",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cantines");

            migrationBuilder.DropTable(
                name: "ParentEnfants");

            migrationBuilder.DropTable(
                name: "TeacherEnfants");

            migrationBuilder.DropIndex(
                name: "IX_Enfants_EcoleId_AnneeScolaire",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "AnneeScolaire",
                table: "Enfants");

            migrationBuilder.AddColumn<bool>(
                name: "CantinePaye",
                table: "Enfants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NumeroEtudiant",
                table: "Enfants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentId",
                table: "Enfants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Paiements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcoleId = table.Column<int>(type: "integer", nullable: false),
                    EnfantId = table.Column<int>(type: "integer", nullable: false),
                    AnneeScolaire = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    DateEcheance = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DatePaiement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModePaiement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    NumeroPiece = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "en_attente"),
                    Trimestre = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    TypePaiement = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paiements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paiements_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Paiements_Enfants_EnfantId",
                        column: x => x.EnfantId,
                        principalTable: "Enfants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enfants_EcoleId_NumeroEtudiant",
                table: "Enfants",
                columns: new[] { "EcoleId", "NumeroEtudiant" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enfants_ParentId",
                table: "Enfants",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_EcoleId",
                table: "Paiements",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_EcoleId_TypePaiement_Statut",
                table: "Paiements",
                columns: new[] { "EcoleId", "TypePaiement", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_EnfantId",
                table: "Paiements",
                column: "EnfantId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_EnfantId_Trimestre_AnneeScolaire",
                table: "Paiements",
                columns: new[] { "EnfantId", "Trimestre", "AnneeScolaire" });

            migrationBuilder.AddForeignKey(
                name: "FK_Enfants_Users_ParentId",
                table: "Enfants",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
