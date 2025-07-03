using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InstitutFroebel.API.Migrations
{
    /// <inheritdoc />
    public partial class FixEntityReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Annonces_Users_CreatedById",
                table: "Annonces");

            migrationBuilder.DropForeignKey(
                name: "FK_Bulletins_Users_CreatedById",
                table: "Bulletins");

            migrationBuilder.DropForeignKey(
                name: "FK_CahierLiaisons_Users_CreatedById",
                table: "CahierLiaisons");

            migrationBuilder.DropForeignKey(
                name: "FK_Enfants_Users_ParentId1",
                table: "Enfants");

            migrationBuilder.DropTable(
                name: "Cantines");

            migrationBuilder.DropIndex(
                name: "IX_Enfants_ParentId1",
                table: "Enfants");

            migrationBuilder.DropIndex(
                name: "IX_Bulletins_CreatedById",
                table: "Bulletins");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Classe",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "Niveau",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "ParentId1",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "CheminFichier",
                table: "Bulletins");

            migrationBuilder.DropColumn(
                name: "DatePublication",
                table: "Bulletins");

            migrationBuilder.DropColumn(
                name: "MoyenneGenerale",
                table: "Bulletins");

            migrationBuilder.DropColumn(
                name: "VisibleParent",
                table: "Bulletins");

            migrationBuilder.DropColumn(
                name: "DateExpiration",
                table: "Annonces");

            migrationBuilder.RenameColumn(
                name: "Visible",
                table: "Annonces",
                newName: "EnvoyerNotification");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateNaissance",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sexe",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "CantinePaye",
                table: "Enfants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClasseId",
                table: "Enfants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Enfants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateInscription",
                table: "Enfants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "Enfants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UtiliseCantine",
                table: "Enfants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AnneeScolaire",
                table: "Ecoles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "2024-2025");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Ecoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "Ecoles",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "CahierLiaisons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateReponse",
                table: "CahierLiaisons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fichiers",
                table: "CahierLiaisons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReponseParent",
                table: "CahierLiaisons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReponseRequise",
                table: "CahierLiaisons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "CahierLiaisons",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NomFichier",
                table: "Bulletins",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "FichierBulletin",
                table: "Bulletins",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "Bulletins",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "Annonces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ClasseCible",
                table: "Annonces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fichiers",
                table: "Annonces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "Annonces",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClasseConcernee",
                table: "Activites",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HeureDebut",
                table: "Activites",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HeureFin",
                table: "Activites",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedById",
                table: "Activites",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Effectif = table.Column<int>(type: "integer", nullable: false),
                    EnseignantPrincipalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classes_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Users_EnseignantPrincipalId",
                        column: x => x.EnseignantPrincipalId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Paiements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnfantId = table.Column<int>(type: "integer", nullable: false),
                    TypePaiement = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DatePaiement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModePaiement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "en_attente"),
                    NumeroPiece = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Trimestre = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    AnneeScolaire = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Emplois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClasseId = table.Column<int>(type: "integer", nullable: false),
                    NomFichier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FichierEmploi = table.Column<byte[]>(type: "bytea", nullable: false),
                    AnneeScolaire = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    UpdatedById = table.Column<string>(type: "text", nullable: true),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emplois", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emplois_Classes_ClasseId",
                        column: x => x.ClasseId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Emplois_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enfants_ClasseId",
                table: "Enfants",
                column: "ClasseId");

            migrationBuilder.CreateIndex(
                name: "IX_CahierLiaisons_EcoleId_CreatedAt",
                table: "CahierLiaisons",
                columns: new[] { "EcoleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_EcoleId",
                table: "Classes",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_EcoleId_Nom",
                table: "Classes",
                columns: new[] { "EcoleId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_EnseignantPrincipalId",
                table: "Classes",
                column: "EnseignantPrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_Emplois_ClasseId",
                table: "Emplois",
                column: "ClasseId");

            migrationBuilder.CreateIndex(
                name: "IX_Emplois_ClasseId_AnneeScolaire_NomFichier",
                table: "Emplois",
                columns: new[] { "ClasseId", "AnneeScolaire", "NomFichier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Emplois_EcoleId",
                table: "Emplois",
                column: "EcoleId");

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
                name: "FK_Annonces_Users_CreatedById",
                table: "Annonces",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CahierLiaisons_Users_CreatedById",
                table: "CahierLiaisons",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enfants_Classes_ClasseId",
                table: "Enfants",
                column: "ClasseId",
                principalTable: "Classes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Annonces_Users_CreatedById",
                table: "Annonces");

            migrationBuilder.DropForeignKey(
                name: "FK_CahierLiaisons_Users_CreatedById",
                table: "CahierLiaisons");

            migrationBuilder.DropForeignKey(
                name: "FK_Enfants_Classes_ClasseId",
                table: "Enfants");

            migrationBuilder.DropTable(
                name: "Emplois");

            migrationBuilder.DropTable(
                name: "Paiements");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Enfants_ClasseId",
                table: "Enfants");

            migrationBuilder.DropIndex(
                name: "IX_CahierLiaisons_EcoleId_CreatedAt",
                table: "CahierLiaisons");

            migrationBuilder.DropColumn(
                name: "DateNaissance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Sexe",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CantinePaye",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "ClasseId",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "DateInscription",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "UtiliseCantine",
                table: "Enfants");

            migrationBuilder.DropColumn(
                name: "AnneeScolaire",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Ecoles");

            migrationBuilder.DropColumn(
                name: "DateReponse",
                table: "CahierLiaisons");

            migrationBuilder.DropColumn(
                name: "Fichiers",
                table: "CahierLiaisons");

            migrationBuilder.DropColumn(
                name: "ReponseParent",
                table: "CahierLiaisons");

            migrationBuilder.DropColumn(
                name: "ReponseRequise",
                table: "CahierLiaisons");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "CahierLiaisons");

            migrationBuilder.DropColumn(
                name: "FichierBulletin",
                table: "Bulletins");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Bulletins");

            migrationBuilder.DropColumn(
                name: "Fichiers",
                table: "Annonces");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Annonces");

            migrationBuilder.DropColumn(
                name: "HeureDebut",
                table: "Activites");

            migrationBuilder.DropColumn(
                name: "HeureFin",
                table: "Activites");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "Activites");

            migrationBuilder.RenameColumn(
                name: "EnvoyerNotification",
                table: "Annonces",
                newName: "Visible");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Classe",
                table: "Enfants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Niveau",
                table: "Enfants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentId1",
                table: "Enfants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "CahierLiaisons",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NomFichier",
                table: "Bulletins",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "CheminFichier",
                table: "Bulletins",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePublication",
                table: "Bulletins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MoyenneGenerale",
                table: "Bulletins",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleParent",
                table: "Bulletins",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "Annonces",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClasseCible",
                table: "Annonces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateExpiration",
                table: "Annonces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClasseConcernee",
                table: "Activites",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Cantines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EcoleId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateMenu = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Menu = table.Column<string>(type: "text", nullable: false),
                    Prix = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Semaine = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TypeRepas = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "dejeuner"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Enfants_ParentId1",
                table: "Enfants",
                column: "ParentId1");

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_CreatedById",
                table: "Bulletins",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Cantines_EcoleId",
                table: "Cantines",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cantines_EcoleId_DateMenu_TypeRepas",
                table: "Cantines",
                columns: new[] { "EcoleId", "DateMenu", "TypeRepas" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Annonces_Users_CreatedById",
                table: "Annonces",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bulletins_Users_CreatedById",
                table: "Bulletins",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CahierLiaisons_Users_CreatedById",
                table: "CahierLiaisons",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enfants_Users_ParentId1",
                table: "Enfants",
                column: "ParentId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
