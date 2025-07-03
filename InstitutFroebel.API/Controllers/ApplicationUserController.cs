using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.Identity;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using OfficeOpenXml;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/users")]
    [ApiController]
    [Authorize]
    public class ApplicationUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ApplicationUserController> _logger;

        public ApplicationUserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ApplicationUserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/users
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<IEnumerable<UserListeDto>>> GetUsers(
            int ecoleId,
            [FromQuery] string? role = null,
            [FromQuery] string? recherche = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var query = _context.Users
                    .Where(u => u.EcoleId == ecoleId);

                // Filtrage par rôle
                if (!string.IsNullOrEmpty(role))
                {
                    var roleIds = await _context.Roles
                        .Where(r => r.Name == role)
                        .Select(r => r.Id)
                        .ToListAsync();

                    if (roleIds.Any())
                    {
                        query = query.Where(u => _context.UserRoles
                            .Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId)));
                    }
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(u => u.Nom.ToLower().Contains(termeLower) ||
                                           u.Prenom.ToLower().Contains(termeLower) ||
                                           u.Email.ToLower().Contains(termeLower) ||
                                           (u.Telephone != null && u.Telephone.Contains(recherche)));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var users = await query
                    .OrderBy(u => u.Nom)
                    .ThenBy(u => u.Prenom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .Include(u => u.EnfantsAsTeacher)
                        .ThenInclude(te => te.Enfant)
                    .Include(u => u.ClassesEnseignees.Where(c => !c.IsDeleted))
                    .ToListAsync();

                var userDtos = new List<UserListeDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    userDtos.Add(new UserListeDto
                    {
                        Id = user.Id,
                        Nom = user.Nom,
                        Prenom = user.Prenom,
                        Email = user.Email ?? string.Empty,
                        Telephone = user.Telephone,
                        Sexe = user.Sexe,
                        DateNaissance = user.DateNaissance,
                        Roles = roles.ToList(),
                        NombreEnfants = user.EnfantsAsParent.Count(pe => !pe.Enfant.IsDeleted) + user.EnfantsAsTeacher.Count(te => !te.Enfant.IsDeleted),
                        NombreClassesEnseignees = user.ClassesEnseignees.Count,
                        EmailConfirmed = user.EmailConfirmed,
                        CreatedAt = user.CreatedAt
                    });
                }

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des utilisateurs");
            }
        }

        // GET: api/ecoles/{ecoleId}/users/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<UserDetailDto>> GetUser(int ecoleId, string id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("L'identifiant de l'utilisateur est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var user = await _context.Users
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .Include(u => u.EnfantsAsTeacher)
                        .ThenInclude(te => te.Enfant)
                    .Include(u => u.ClassesEnseignees.Where(c => !c.IsDeleted))
                    .Include(u => u.Ecole)
                    .Where(u => u.Id == id && u.EcoleId == ecoleId)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound($"Utilisateur avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                var roles = await _userManager.GetRolesAsync(user);

                var userDto = new UserDetailDto
                {
                    Id = user.Id,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email ?? string.Empty,
                    Telephone = user.Telephone,
                    Adresse = user.Adresse,
                    Sexe = user.Sexe,
                    DateNaissance = user.DateNaissance,
                    Roles = roles.ToList(),
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount,
                    EcoleNom = user.Ecole?.Nom ?? string.Empty,
                    Enfants = user.EnfantsAsParent.Where(pe => !pe.Enfant.IsDeleted).Select(pe => new EnfantSimpleDto
                    {
                        Id = pe.Enfant.Id,
                        Nom = pe.Enfant.Nom,
                        Prenom = pe.Enfant.Prenom,
                        DateNaissance = pe.Enfant.DateNaissance,
                        Statut = pe.Enfant.Statut
                    }).Concat(user.EnfantsAsTeacher.Where(te => !te.Enfant.IsDeleted).Select(te => new EnfantSimpleDto
                    {
                        Id = te.Enfant.Id,
                        Nom = te.Enfant.Nom,
                        Prenom = te.Enfant.Prenom,
                        DateNaissance = te.Enfant.DateNaissance,
                        Statut = te.Enfant.Statut
                    })).ToList(),
                    ClassesEnseignees = user.ClassesEnseignees.Select(c => new ClasseSimpleDto
                    {
                        Id = c.Id,
                        Nom = c.Nom,
                        Effectif = c.Effectif
                    }).ToList(),
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur {UserId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'utilisateur");
            }
        }

        // POST: api/ecoles/{ecoleId}/users
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<UserDetailDto>> CreateUser(int ecoleId, [FromBody] CreateUserRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                // Vérifier que l'école existe
                var ecole = await _context.Ecoles.FindAsync(ecoleId);
                if (ecole == null)
                {
                    return NotFound("École non trouvée");
                }

                // Vérifier l'unicité de l'email dans cette école
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Email == request.Email && u.EcoleId == ecoleId);

                if (existingUser)
                {
                    return BadRequest($"Un utilisateur avec l'email '{request.Email}' existe déjà dans cette école");
                }

                // Vérifier que le rôle existe
                var roleExists = await _context.Roles.AnyAsync(r => r.Name == request.Role);
                if (!roleExists)
                {
                    return BadRequest($"Le rôle '{request.Role}' n'existe pas");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    Telephone = request.Telephone,
                    Adresse = request.Adresse,
                    Sexe = request.Sexe,
                    DateNaissance = request.DateNaissance,
                    EcoleId = ecoleId,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, request.MotDePasse);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { Message = "Erreur lors de la création de l'utilisateur", Errors = errors });
                }

                // Assigner le rôle
                await _userManager.AddToRoleAsync(user, request.Role);

                // Recharger l'utilisateur avec les relations
                user = await _context.Users
                    .Include(u => u.Ecole)
                    .FirstAsync(u => u.Id == user.Id);

                var roles = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("Utilisateur '{Email}' créé dans l'école {EcoleId} avec le rôle {Role}",
                    user.Email, ecoleId, request.Role);

                var userDto = new UserDetailDto
                {
                    Id = user.Id,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email ?? string.Empty,
                    Telephone = user.Telephone,
                    Adresse = user.Adresse,
                    Sexe = user.Sexe,
                    DateNaissance = user.DateNaissance,
                    Roles = roles.ToList(),
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount,
                    EcoleNom = user.Ecole?.Nom ?? string.Empty,
                    Enfants = new List<EnfantSimpleDto>(),
                    ClassesEnseignees = new List<ClasseSimpleDto>(),
                    CreatedAt = user.CreatedAt
                };

                return CreatedAtAction(nameof(GetUser), new { ecoleId, id = user.Id }, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'utilisateur dans l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'utilisateur");
            }
        }

        // PUT: api/ecoles/{ecoleId}/users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateUser(int ecoleId, string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("L'identifiant de l'utilisateur est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.EcoleId == ecoleId);

                if (user == null)
                {
                    return NotFound($"Utilisateur avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                // Vérifier l'unicité de l'email si modifié
                if (!string.IsNullOrEmpty(request.Email) &&
                    request.Email.ToLower() != user.Email?.ToLower())
                {
                    var existingEmail = await _context.Users
                        .AnyAsync(u => u.Id != id &&
                                     u.Email == request.Email &&
                                     u.EcoleId == ecoleId);

                    if (existingEmail)
                    {
                        return BadRequest($"Un utilisateur avec l'email '{request.Email}' existe déjà dans cette école");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom))
                    user.Nom = request.Nom;
                if (!string.IsNullOrEmpty(request.Prenom))
                    user.Prenom = request.Prenom;
                if (!string.IsNullOrEmpty(request.Email))
                {
                    user.Email = request.Email;
                    user.UserName = request.Email;
                }
                if (!string.IsNullOrEmpty(request.Telephone))
                    user.Telephone = request.Telephone;
                if (!string.IsNullOrEmpty(request.Adresse))
                    user.Adresse = request.Adresse;
                if (!string.IsNullOrEmpty(request.Sexe))
                    user.Sexe = request.Sexe;
                if (request.DateNaissance.HasValue)
                    user.DateNaissance = request.DateNaissance;

                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { Message = "Erreur lors de la mise à jour", Errors = errors });
                }

                _logger.LogInformation("Utilisateur {UserId} mis à jour dans l'école {EcoleId}", id, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'utilisateur {UserId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'utilisateur");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteUser(int ecoleId, string id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("L'identifiant de l'utilisateur est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == id)
                {
                    return BadRequest("Impossible de supprimer votre propre compte");
                }

                var user = await _context.Users
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .Include(u => u.EnfantsAsTeacher)
                        .ThenInclude(te => te.Enfant)
                    .FirstOrDefaultAsync(u => u.Id == id && u.EcoleId == ecoleId);

                if (user == null)
                {
                    return NotFound($"Utilisateur avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                // Vérifier s'il y a des enfants associés
                var hasActiveChildren = user.EnfantsAsParent.Any(pe => !pe.Enfant.IsDeleted) || user.EnfantsAsTeacher.Any(te => !te.Enfant.IsDeleted);
                if (hasActiveChildren)
                {
                    return BadRequest("Impossible de supprimer un utilisateur qui a des enfants associés");
                }

                // Supprimer l'utilisateur (pas de soft delete pour les utilisateurs)
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { Message = "Erreur lors de la suppression de l'utilisateur", Errors = errors });
                }

                _logger.LogInformation("Utilisateur '{Email}' supprimé de l'école {EcoleId}", user.Email, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'utilisateur {UserId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'utilisateur");
            }
        }

        // POST: api/ecoles/{ecoleId}/users/{id}/reset-password
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ResetPassword(int ecoleId, string id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("L'identifiant de l'utilisateur est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.EcoleId == ecoleId);

                if (user == null)
                {
                    return NotFound($"Utilisateur avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                // Réinitialiser le mot de passe
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, request.NouveauMotDePasse);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { Message = "Erreur lors de la réinitialisation du mot de passe", Errors = errors });
                }

                _logger.LogInformation("Mot de passe réinitialisé pour l'utilisateur {UserId} de l'école {EcoleId}", id, ecoleId);

                return Ok(new { Message = "Mot de passe réinitialisé avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réinitialisation du mot de passe pour l'utilisateur {UserId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la réinitialisation du mot de passe");
            }
        }

        // GET: api/ecoles/{ecoleId}/users/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<UserStatistiquesDto>> GetStatistiques(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var users = await _context.Users
                    .Where(u => u.EcoleId == ecoleId)
                    .ToListAsync();

                var statistiques = new UserStatistiquesDto
                {
                    EcoleId = ecoleId,
                    NombreTotalUtilisateurs = users.Count
                };

                // Statistiques par rôle
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    foreach (var role in roles)
                    {
                        switch (role)
                        {
                            case "Admin":
                                statistiques.NombreAdmins++;
                                break;
                            case "Teacher":
                                statistiques.NombreEnseignants++;
                                break;
                            case "Parent":
                                statistiques.NombreParents++;
                                break;
                        }
                    }
                }

                // Statistiques d'activité
                statistiques.NombreUtilisateursActifs = users.Count(u => u.EmailConfirmed);
                statistiques.NombreUtilisateursInactifs = users.Count(u => !u.EmailConfirmed);

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des utilisateurs de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/ecoles/{ecoleId}/users/export-excel
        [HttpGet("export-excel")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ExportUsersExcel(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var users = await _context.Users
                    .Where(u => u.EcoleId == ecoleId)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .Include(u => u.EnfantsAsTeacher)
                        .ThenInclude(te => te.Enfant)
                    .Include(u => u.ClassesEnseignees.Where(c => !c.IsDeleted))
                    .OrderBy(u => u.Nom)
                    .ThenBy(u => u.Prenom)
                    .ToListAsync();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Utilisateurs");

                // En-têtes
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nom";
                worksheet.Cells[1, 3].Value = "Prénom";
                worksheet.Cells[1, 4].Value = "Email";
                worksheet.Cells[1, 5].Value = "Téléphone";
                worksheet.Cells[1, 6].Value = "Sexe";
                worksheet.Cells[1, 7].Value = "Date Naissance";
                worksheet.Cells[1, 8].Value = "Rôles";
                worksheet.Cells[1, 9].Value = "Nb Enfants";
                worksheet.Cells[1, 10].Value = "Nb Classes";
                worksheet.Cells[1, 11].Value = "Email Confirmé";
                worksheet.Cells[1, 12].Value = "Date Création";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 12])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                for (int i = 0; i < users.Count; i++)
                {
                    var user = users[i];
                    var row = i + 2;
                    var roles = await _userManager.GetRolesAsync(user);

                    worksheet.Cells[row, 1].Value = user.Id;
                    worksheet.Cells[row, 2].Value = user.Nom;
                    worksheet.Cells[row, 3].Value = user.Prenom;
                    worksheet.Cells[row, 4].Value = user.Email;
                    worksheet.Cells[row, 5].Value = user.Telephone;
                    worksheet.Cells[row, 6].Value = user.Sexe;
                    worksheet.Cells[row, 7].Value = user.DateNaissance?.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 8].Value = string.Join(", ", roles);
                    worksheet.Cells[row, 9].Value = user.EnfantsAsParent.Count(pe => !pe.Enfant.IsDeleted) + user.EnfantsAsTeacher.Count(te => !te.Enfant.IsDeleted);
                    worksheet.Cells[row, 10].Value = user.ClassesEnseignees.Count;
                    worksheet.Cells[row, 11].Value = user.EmailConfirmed ? "Oui" : "Non";
                    worksheet.Cells[row, 12].Value = user.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                }

                // Auto-fit des colonnes
                worksheet.Cells.AutoFitColumns();

                var ecole = await _context.Ecoles.FindAsync(ecoleId);
                var fileBytes = package.GetAsByteArray();
                var fileName = $"Utilisateurs_{ecole?.Code}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Excel des utilisateurs de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de l'export Excel");
            }
        }

        // Méthodes d'aide
        private async Task<bool> CanAccessEcole(int ecoleId)
        {
            if (User.IsInRole("SuperAdmin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _context.Users.FindAsync(userId);
            return user != null && user.EcoleId == ecoleId;
        }

        private async Task<bool> CanManageEcole(int ecoleId)
        {
            if (User.IsInRole("SuperAdmin"))
                return true;

            if (User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return false;

                var user = await _context.Users.FindAsync(userId);
                return user != null && user.EcoleId == ecoleId;
            }

            return false;
        }
    }

    // DTOs pour les utilisateurs
    public class UserListeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public DateTime? DateNaissance { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int NombreEnfants { get; set; }
        public int NombreClassesEnseignees { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserDetailDto : UserListeDto
    {
        public string? Adresse { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public List<EnfantSimpleDto> Enfants { get; set; } = new List<EnfantSimpleDto>();
        public List<ClasseSimpleDto> ClassesEnseignees { get; set; } = new List<ClasseSimpleDto>();
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string Email { get; set; } = string.Empty;

        public string? Telephone { get; set; }
        public string? Adresse { get; set; }

        [Required(ErrorMessage = "Le sexe est obligatoire")]
        public string Sexe { get; set; } = string.Empty;

        public DateTime? DateNaissance { get; set; }

        [Required(ErrorMessage = "Le rôle est obligatoire")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        public string MotDePasse { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string? Nom { get; set; }
        public string? Prenom { get; set; }

        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string? Email { get; set; }

        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string? Sexe { get; set; }
        public DateTime? DateNaissance { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
        public string NouveauMotDePasse { get; set; } = string.Empty;
    }

    public class UserStatistiquesDto
    {
        public int EcoleId { get; set; }
        public int NombreTotalUtilisateurs { get; set; }
        public int NombreAdmins { get; set; }
        public int NombreEnseignants { get; set; }
        public int NombreParents { get; set; }
        public int NombreUtilisateursActifs { get; set; }
        public int NombreUtilisateursInactifs { get; set; }
    }

    public class EnfantSimpleDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Statut { get; set; } = string.Empty;
    }

    public class ClasseSimpleDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int Effectif { get; set; }
    }
}