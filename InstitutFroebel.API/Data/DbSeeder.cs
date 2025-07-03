using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.Core.Entities.Identity;
using InstitutFroebel.Core.Entities.School;

namespace InstitutFroebel.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            try
            {
                // Créer les rôles s'ils n'existent pas
                await CreateRolesAsync(roleManager);
                Console.WriteLine("✅ Rôles créés");

                // Créer une école par défaut
                var defaultSchool = await CreateDefaultSchoolAsync(context);
                Console.WriteLine("✅ École par défaut créée");

                // Créer un utilisateur SuperAdmin par défaut
                await CreateDefaultSuperAdminAsync(userManager, defaultSchool);
                Console.WriteLine("✅ SuperAdmin par défaut créé");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du seeding: {ex.Message}");
            }
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "SuperAdmin", "Admin", "Teacher", "Parent" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"Rôle créé: {role}");
                }
            }
        }

        private static async Task<Ecole> CreateDefaultSchoolAsync(ApplicationDbContext context)
        {
            // Vérifier si une école par défaut existe déjà
            var existingSchool = await context.Ecoles
                .FirstOrDefaultAsync(s => s.Code == "FROEBEL_DEFAULT");

            if (existingSchool != null)
            {
                return existingSchool;
            }

            var school = new Ecole
            {
                Nom = "Institut Froebel LA TULIPE",
                Code = "FROEBEL_DEFAULT",
                Adresse = "Marcory Anoumambo, en face de l'ARTCI",
                Commune = "Marcory",
                Telephone = "+225 27 22 49 50 00",
                Email = "contact@froebel-default.ci",
                AnneeScolaire = "2024-2025",
                CreatedAt = DateTime.UtcNow
            };

            context.Ecoles.Add(school);
            await context.SaveChangesAsync();

            Console.WriteLine($"École créée: {school.Nom} (Code: {school.Code})");
            return school;
        }

        private static async Task CreateDefaultSuperAdminAsync(UserManager<ApplicationUser> userManager, Ecole school)
        {
            // Vérifier si un SuperAdmin existe déjà
            var existingSuperAdmin = await userManager.GetUsersInRoleAsync("SuperAdmin");
            if (existingSuperAdmin.Any())
            {
                Console.WriteLine("SuperAdmin existe déjà, pas de création");
                return;
            }

            var superAdmin = new ApplicationUser
            {
                UserName = "superadmin@froebel.ci",
                Email = "superadmin@froebel.ci",
                Nom = "Super",
                Prenom = "Admin",
                Telephone = "+225 27 22 49 50 01",
                Adresse = "123 Avenue de la Paix, Abidjan",
                Sexe = "M",
                DateNaissance = new DateTime(1980, 1, 1),
                EcoleId = school.Id,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(superAdmin, "SuperAdmin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                Console.WriteLine($"SuperAdmin créé: {superAdmin.Email}");
                Console.WriteLine("Mot de passe: SuperAdmin123!");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Erreur lors de la création du SuperAdmin: {errors}");
            }
        }
    }
}