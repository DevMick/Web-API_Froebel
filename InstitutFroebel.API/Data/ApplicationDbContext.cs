using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.Core.Entities.Identity;
using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.Core.Interfaces;
using InstitutFroebel.Core.Entities.Base;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace InstitutFroebel.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // SUPPRIMÉ : Plus d'injection de ITenantService ici
        }

        // DbSets mis à jour selon les nouveaux modèles
        public DbSet<Ecole> Ecoles { get; set; }
        public DbSet<Classe> Classes { get; set; }
        public DbSet<Enfant> Enfants { get; set; }
        public DbSet<Emploi> Emplois { get; set; }
        public DbSet<Bulletin> Bulletins { get; set; }
        public DbSet<CahierLiaison> CahierLiaisons { get; set; }
        public DbSet<Annonce> Annonces { get; set; }
        public DbSet<Activite> Activites { get; set; }
        public DbSet<Cantine> Cantines { get; set; }

        // NOUVELLES TABLES DE LIAISON
        public DbSet<ParentEnfant> ParentEnfants { get; set; }
        public DbSet<TeacherEnfant> TeacherEnfants { get; set; }

        // SUPPRIMÉ: Paiements

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuration des entités
            ConfigureIdentityTables(builder);
            ConfigureSchoolEntities(builder);
            ConfigureLiaisonTables(builder); // NOUVEAU

            SeedData(builder);
        }

        private void ConfigureIdentityTables(ModelBuilder builder)
        {
            // Renommer les tables Identity pour plus de clarté
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

            // Configuration ApplicationUser (mise à jour)
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Prenom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Telephone).HasMaxLength(20);
                entity.Property(e => e.Adresse).HasMaxLength(500);

                // Index pour améliorer les performances
                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => new { e.EcoleId, e.Email }).IsUnique();

                // Relation avec École
                entity.HasOne(e => e.Ecole)
                      .WithMany(s => s.Users)
                      .HasForeignKey(e => e.EcoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureSchoolEntities(ModelBuilder builder)
        {
            // Configuration École (inchangé)
            builder.Entity<Ecole>(entity =>
            {
                entity.Property(e => e.Nom).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Telephone).HasMaxLength(20);
                entity.Property(e => e.Adresse).HasMaxLength(500);
                entity.Property(e => e.Commune).HasMaxLength(100);
                entity.Property(e => e.AnneeScolaire).HasMaxLength(20).HasDefaultValue("2024-2025");

                // Index unique sur le code et email
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configuration Classe (inchangé)
            builder.Entity<Classe>(entity =>
            {
                entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Effectif).IsRequired();

                // Index
                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => new { e.EcoleId, e.Nom }).IsUnique();

                // Relation avec École
                entity.HasOne(e => e.Ecole)
                      .WithMany(s => s.Classes)
                      .HasForeignKey(e => e.EcoleId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation avec Enseignant Principal (optionnel)
                entity.HasOne(e => e.EnseignantPrincipal)
                      .WithMany(u => u.ClassesEnseignees)
                      .HasForeignKey(e => e.EnseignantPrincipalId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuration Enfant (MISE À JOUR)
            builder.Entity<Enfant>(entity =>
            {
                entity.Property(e => e.Nom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Prenom).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Sexe).HasMaxLength(1).IsRequired();
                entity.Property(e => e.Statut).HasMaxLength(20).HasDefaultValue("pre_inscrit");
                entity.Property(e => e.AnneeScolaire).HasMaxLength(20).IsRequired(); // AJOUTÉ
                // SUPPRIMÉ: NumeroEtudiant, ParentId

                // Index mis à jour
                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => e.ClasseId);
                entity.HasIndex(e => new { e.EcoleId, e.AnneeScolaire }); // NOUVEAU
                // SUPPRIMÉ: Index sur ParentId et NumeroEtudiant

                // Relations mises à jour
                // SUPPRIMÉ: Relation directe avec Parent
                entity.HasOne(e => e.Classe)
                      .WithMany(c => c.Enfants)
                      .HasForeignKey(e => e.ClasseId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuration Emploi (inchangé)
            builder.Entity<Emploi>(entity =>
            {
                entity.Property(e => e.NomFichier).HasMaxLength(255).IsRequired();
                entity.Property(e => e.AnneeScolaire).HasMaxLength(20).IsRequired();
                entity.Property(e => e.FichierEmploi).IsRequired();

                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => e.ClasseId);
                entity.HasIndex(e => new { e.ClasseId, e.AnneeScolaire, e.NomFichier }).IsUnique();

                // Relations
                entity.HasOne(e => e.Classe)
                      .WithMany(c => c.EmploisDuTemps)
                      .HasForeignKey(e => e.ClasseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration Bulletin (inchangé)
            builder.Entity<Bulletin>(entity =>
            {
                entity.Property(e => e.Trimestre).HasMaxLength(5).IsRequired();
                entity.Property(e => e.AnneeScolaire).HasMaxLength(20).IsRequired();
                entity.Property(e => e.NomFichier).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FichierBulletin).IsRequired();

                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => e.EnfantId);
                entity.HasIndex(e => new { e.EnfantId, e.Trimestre, e.AnneeScolaire }).IsUnique();

                // Relations
                entity.HasOne(e => e.Enfant)
                      .WithMany(en => en.Bulletins)
                      .HasForeignKey(e => e.EnfantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration CahierLiaison (inchangé)
            builder.Entity<CahierLiaison>(entity =>
            {
                entity.Property(e => e.Titre).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(20).HasDefaultValue("info");
                entity.Property(e => e.Message).IsRequired();

                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => e.EnfantId);
                entity.HasIndex(e => new { e.EcoleId, e.CreatedAt });

                // Relations
                entity.HasOne(e => e.Enfant)
                      .WithMany(en => en.MessagesLiaison)
                      .HasForeignKey(e => e.EnfantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration Annonce (inchangé)
            builder.Entity<Annonce>(entity =>
            {
                entity.Property(e => e.Titre).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Type).HasMaxLength(20).HasDefaultValue("generale");
                entity.Property(e => e.Contenu).IsRequired();

                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => new { e.EcoleId, e.DatePublication });
            });

            // Configuration Activité (inchangé)
            builder.Entity<Activite>(entity =>
            {
                entity.Property(e => e.Nom).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Lieu).HasMaxLength(255);

                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => new { e.EcoleId, e.DateDebut });
            });

            // Configuration Cantine (ajouté si manquant)
            builder.Entity<Cantine>(entity =>
            {
                entity.Property(e => e.TypeRepas).HasMaxLength(20).HasDefaultValue("dejeuner");
                entity.Property(e => e.Menu).IsRequired();
                entity.Property(e => e.Semaine).HasMaxLength(50);
                entity.Property(e => e.Prix).HasPrecision(10, 2);

                entity.HasIndex(e => e.EcoleId);
                entity.HasIndex(e => new { e.EcoleId, e.DateMenu });
            });

            // SUPPRIMÉ: Configuration Paiement
        }

        // NOUVELLE MÉTHODE pour configurer les tables de liaison
        private void ConfigureLiaisonTables(ModelBuilder builder)
        {
            // Configuration ParentEnfant
            builder.Entity<ParentEnfant>(entity =>
            {
                // Clé composite
                entity.HasKey(pe => new { pe.ParentId, pe.EnfantId });

                // Index
                entity.HasIndex(pe => pe.EcoleId);
                entity.HasIndex(pe => pe.ParentId);
                entity.HasIndex(pe => pe.EnfantId);

                // Relations
                entity.HasOne(pe => pe.Parent)
                      .WithMany(p => p.EnfantsAsParent)
                      .HasForeignKey(pe => pe.ParentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pe => pe.Enfant)
                      .WithMany(e => e.ParentsEnfants)
                      .HasForeignKey(pe => pe.EnfantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration TeacherEnfant
            builder.Entity<TeacherEnfant>(entity =>
            {
                // Clé composite
                entity.HasKey(te => new { te.TeacherId, te.EnfantId });

                // Index
                entity.HasIndex(te => te.EcoleId);
                entity.HasIndex(te => te.TeacherId);
                entity.HasIndex(te => te.EnfantId);

                // Relations
                entity.HasOne(te => te.Teacher)
                      .WithMany(t => t.EnfantsAsTeacher)
                      .HasForeignKey(te => te.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(te => te.Enfant)
                      .WithMany(e => e.TeachersEnfants)
                      .HasForeignKey(te => te.EnfantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void SeedData(ModelBuilder builder)
        {
            // Seed des rôles avec des valeurs FIXES
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().HasData(
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "1",
                    Name = "SuperAdmin",
                    NormalizedName = "SUPERADMIN",
                    ConcurrencyStamp = "superadmin-stamp-v1" // Valeur fixe
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "2",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "admin-stamp-v1" // Valeur fixe
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "3",
                    Name = "Teacher",
                    NormalizedName = "TEACHER",
                    ConcurrencyStamp = "teacher-stamp-v1" // Valeur fixe
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "4",
                    Name = "Parent",
                    NormalizedName = "PARENT",
                    ConcurrencyStamp = "parent-stamp-v1" // Valeur fixe
                }
            );
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // CHANGÉ : Récupération du TenantService depuis le contexte HTTP
            // au lieu de l'injection dans le constructeur
            var httpContextAccessor = this.GetService<IHttpContextAccessor>();
            var tenantService = httpContextAccessor?.HttpContext?.RequestServices?.GetService<ITenantService>();

            // Ajouter automatiquement l'EcoleId pour les nouvelles entités
            if (tenantService?.GetCurrentTenantId() != null)
            {
                var tenantId = tenantService.GetCurrentTenantId().Value;

                foreach (var entry in ChangeTracker.Entries<InstitutFroebel.Core.Entities.Base.ITenantEntity>())
                {
                    if (entry.State == EntityState.Added)
                    {
                        entry.Entity.EcoleId = tenantId;
                    }
                }
            }

            // Mise à jour automatique des timestamps
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}