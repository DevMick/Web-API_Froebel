using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InstitutFroebel.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ecoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Adresse = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ville = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Pays = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telephone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SiteWeb = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Logo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DateOuverture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Devise = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "FCFA"),
                    Langue = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "fr"),
                    FuseauHoraire = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Africa/Abidjan"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ecoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

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
                    Semaine = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EcoleId = table.Column<int>(type: "integer", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telephone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Adresse = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Activites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DateDebut = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Lieu = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ClasseConcernee = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activites_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activites_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Annonces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "generale"),
                    DatePublication = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateExpiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    ClasseCible = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Annonces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Annonces_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Annonces_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enfants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentId = table.Column<string>(type: "text", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateNaissance = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Sexe = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Classe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Niveau = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pre_inscrit"),
                    NumeroEtudiant = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ParentId1 = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enfants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enfants_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enfants_Users_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enfants_Users_ParentId1",
                        column: x => x.ParentId1,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bulletins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnfantId = table.Column<int>(type: "integer", nullable: false),
                    Trimestre = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    AnneeScolaire = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MoyenneGenerale = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    NomFichier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CheminFichier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DatePublication = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VisibleParent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bulletins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bulletins_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bulletins_Enfants_EnfantId",
                        column: x => x.EnfantId,
                        principalTable: "Enfants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bulletins_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CahierLiaisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnfantId = table.Column<int>(type: "integer", nullable: false),
                    Titre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "info"),
                    LuParParent = table.Column<bool>(type: "boolean", nullable: false),
                    DateLecture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    EcoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CahierLiaisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CahierLiaisons_Ecoles_EcoleId",
                        column: x => x.EcoleId,
                        principalTable: "Ecoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CahierLiaisons_Enfants_EnfantId",
                        column: x => x.EnfantId,
                        principalTable: "Enfants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CahierLiaisons_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Ecoles",
                columns: new[] { "Id", "Adresse", "Code", "CreatedAt", "DateOuverture", "Devise", "Email", "FuseauHoraire", "IsActive", "IsDeleted", "Langue", "Logo", "Nom", "Pays", "SiteWeb", "Telephone", "UpdatedAt", "Ville" },
                values: new object[] { 1, "123 Rue de la Paix", "DEMO_SCHOOL", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "FCFA", "demo@froebel.edu", "Africa/Abidjan", true, false, "fr", null, "Institut Froebel Démonstration", "Côte d'Ivoire", null, "+225 01 02 03 04 05", null, "Abidjan" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1", "superadmin-stamp-v1", "SuperAdmin", "SUPERADMIN" },
                    { "2", "admin-stamp-v1", "Admin", "ADMIN" },
                    { "3", "teacher-stamp-v1", "Teacher", "TEACHER" },
                    { "4", "parent-stamp-v1", "Parent", "PARENT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activites_CreatedById",
                table: "Activites",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Activites_EcoleId",
                table: "Activites",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Activites_EcoleId_DateDebut",
                table: "Activites",
                columns: new[] { "EcoleId", "DateDebut" });

            migrationBuilder.CreateIndex(
                name: "IX_Annonces_CreatedById",
                table: "Annonces",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Annonces_EcoleId",
                table: "Annonces",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Annonces_EcoleId_DatePublication",
                table: "Annonces",
                columns: new[] { "EcoleId", "DatePublication" });

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_CreatedById",
                table: "Bulletins",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_EcoleId",
                table: "Bulletins",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_EnfantId",
                table: "Bulletins",
                column: "EnfantId");

            migrationBuilder.CreateIndex(
                name: "IX_Bulletins_EnfantId_Trimestre_AnneeScolaire",
                table: "Bulletins",
                columns: new[] { "EnfantId", "Trimestre", "AnneeScolaire" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CahierLiaisons_CreatedById",
                table: "CahierLiaisons",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CahierLiaisons_EcoleId",
                table: "CahierLiaisons",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CahierLiaisons_EnfantId",
                table: "CahierLiaisons",
                column: "EnfantId");

            migrationBuilder.CreateIndex(
                name: "IX_Cantines_EcoleId",
                table: "Cantines",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cantines_EcoleId_DateMenu_TypeRepas",
                table: "Cantines",
                columns: new[] { "EcoleId", "DateMenu", "TypeRepas" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ecoles_Code",
                table: "Ecoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ecoles_Email",
                table: "Ecoles",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enfants_EcoleId",
                table: "Enfants",
                column: "EcoleId");

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
                name: "IX_Enfants_ParentId1",
                table: "Enfants",
                column: "ParentId1");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EcoleId",
                table: "Users",
                column: "EcoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EcoleId_Email",
                table: "Users",
                columns: new[] { "EcoleId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activites");

            migrationBuilder.DropTable(
                name: "Annonces");

            migrationBuilder.DropTable(
                name: "Bulletins");

            migrationBuilder.DropTable(
                name: "CahierLiaisons");

            migrationBuilder.DropTable(
                name: "Cantines");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "Enfants");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Ecoles");
        }
    }
}
