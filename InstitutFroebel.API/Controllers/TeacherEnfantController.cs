using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/teacher-enfants")]
    [ApiController]
    [Authorize]
    public class TeacherEnfantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeacherEnfantController> _logger;

        public TeacherEnfantController(
            ApplicationDbContext context,
            ILogger<TeacherEnfantController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/teacher-enfants
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<TeacherEnfantListeDto>>> GetTeacherEnfants(
            int ecoleId,
            [FromQuery] string? teacherId = null,
            [FromQuery] int? enfantId = null,
            [FromQuery] int? classeId = null,
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

                var query = _context.TeacherEnfants
                    .Where(te => te.EcoleId == ecoleId && !te.IsDeleted);

                // Si c'est un enseignant, ne montrer que ses élèves
                if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    query = query.Where(te => te.TeacherId == currentUserId);
                }

                // Filtrage par enseignant
                if (!string.IsNullOrEmpty(teacherId))
                {
                    query = query.Where(te => te.TeacherId == teacherId);
                }

                // Filtrage par enfant
                if (enfantId.HasValue)
                {
                    query = query.Where(te => te.EnfantId == enfantId.Value);
                }

                // Filtrage par classe
                if (classeId.HasValue)
                {
                    query = query.Where(te => te.Enfant.ClasseId == classeId.Value);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(te =>
                        te.Enfant.Nom.ToLower().Contains(termeLower) ||
                        te.Enfant.Prenom.ToLower().Contains(termeLower) ||
                        te.Teacher.Nom.ToLower().Contains(termeLower) ||
                        te.Teacher.Prenom.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var teacherEnfants = await query
                    .Include(te => te.Teacher)
                    .Include(te => te.Enfant)
                        .ThenInclude(e => e.Classe)
                    .OrderBy(te => te.Teacher.Nom)
                        .ThenBy(te => te.Teacher.Prenom)
                        .ThenBy(te => te.Enfant.Nom)
                        .ThenBy(te => te.Enfant.Prenom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(te => new TeacherEnfantListeDto
                    {
                        TeacherId = te.TeacherId,
                        EnfantId = te.EnfantId,
                        TeacherNom = te.Teacher.NomComplet,
                        EnfantNom = $"{te.Enfant.Prenom} {te.Enfant.Nom}",
                        ClasseNom = te.Enfant.Classe != null ? te.Enfant.Classe.Nom : "Non assigné",
                        ClasseId = te.Enfant.ClasseId,
                        AnneeScolaire = te.Enfant.AnneeScolaire,
                        CreatedAt = te.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(teacherEnfants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des relations teacher-enfant de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des relations teacher-enfant");
            }
        }

        // GET: api/ecoles/{ecoleId}/teacher-enfants/{teacherId}/{enfantId}
        [HttpGet("{teacherId}/{enfantId:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<TeacherEnfantDetailDto>> GetTeacherEnfant(int ecoleId, string teacherId, int enfantId)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(teacherId))
                {
                    return BadRequest("L'identifiant de l'enseignant est requis");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessTeacherEnfant(ecoleId, teacherId, enfantId))
                {
                    return Forbid("Accès non autorisé à cette relation");
                }

                var teacherEnfant = await _context.TeacherEnfants
                    .Include(te => te.Teacher)
                    .Include(te => te.Enfant)
                        .ThenInclude(e => e.Classe)
                    .Include(te => te.Ecole)
                    .Where(te => te.TeacherId == teacherId &&
                                te.EnfantId == enfantId &&
                                te.EcoleId == ecoleId &&
                                !te.IsDeleted)
                    .FirstOrDefaultAsync();

                if (teacherEnfant == null)
                {
                    return NotFound($"Relation teacher-enfant non trouvée");
                }

                var result = new TeacherEnfantDetailDto
                {
                    TeacherId = teacherEnfant.TeacherId,
                    EnfantId = teacherEnfant.EnfantId,
                    TeacherNom = teacherEnfant.Teacher.NomComplet,
                    TeacherEmail = teacherEnfant.Teacher.Email,
                    EnfantNom = $"{teacherEnfant.Enfant.Prenom} {teacherEnfant.Enfant.Nom}",
                    EnfantDateNaissance = teacherEnfant.Enfant.DateNaissance,
                    EnfantSexe = teacherEnfant.Enfant.Sexe,
                    ClasseNom = teacherEnfant.Enfant.Classe?.Nom,
                    ClasseId = teacherEnfant.Enfant.ClasseId,
                    AnneeScolaire = teacherEnfant.Enfant.AnneeScolaire,
                    EcoleNom = teacherEnfant.Ecole.Nom,
                    CreatedAt = teacherEnfant.CreatedAt,
                    UpdatedAt = teacherEnfant.UpdatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la relation teacher-enfant {TeacherId}-{EnfantId} de l'école {EcoleId}",
                    teacherId, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la relation");
            }
        }

        // POST: api/ecoles/{ecoleId}/teacher-enfants
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<TeacherEnfantDetailDto>> CreateTeacherEnfant(int ecoleId, [FromBody] CreateTeacherEnfantRequest request)
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

                // Vérifier que l'enseignant existe et appartient à cette école
                var teacher = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.TeacherId &&
                                             u.EcoleId == ecoleId);

                if (teacher == null)
                {
                    return BadRequest("Enseignant non trouvé dans cette école");
                }

                // Vérifier que c'est bien un enseignant
                var isTeacher = await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .AnyAsync(ur => ur.UserId == request.TeacherId && ur.Name == "Teacher");

                if (!isTeacher)
                {
                    return BadRequest("L'utilisateur spécifié n'est pas un enseignant");
                }

                // Vérifier que l'enfant existe et appartient à cette école
                var enfant = await _context.Enfants
                    .FirstOrDefaultAsync(e => e.Id == request.EnfantId &&
                                             e.EcoleId == ecoleId &&
                                             !e.IsDeleted);

                if (enfant == null)
                {
                    return BadRequest("Enfant non trouvé dans cette école");
                }

                // Vérifier que la relation n'existe pas déjà
                var existingRelation = await _context.TeacherEnfants
                    .FirstOrDefaultAsync(te => te.TeacherId == request.TeacherId &&
                                              te.EnfantId == request.EnfantId &&
                                              !te.IsDeleted);

                if (existingRelation != null)
                {
                    return BadRequest("Cette relation teacher-enfant existe déjà");
                }

                var teacherEnfant = new TeacherEnfant
                {
                    TeacherId = request.TeacherId,
                    EnfantId = request.EnfantId,
                    EcoleId = ecoleId
                };

                _context.TeacherEnfants.Add(teacherEnfant);
                await _context.SaveChangesAsync();

                // Recharger avec les relations
                teacherEnfant = await _context.TeacherEnfants
                    .Include(te => te.Teacher)
                    .Include(te => te.Enfant)
                        .ThenInclude(e => e.Classe)
                    .Include(te => te.Ecole)
                    .FirstAsync(te => te.TeacherId == teacherEnfant.TeacherId &&
                                     te.EnfantId == teacherEnfant.EnfantId);

                _logger.LogInformation("Relation teacher-enfant créée: {TeacherId} -> {EnfantId} dans l'école {EcoleId}",
                    request.TeacherId, request.EnfantId, ecoleId);

                var result = new TeacherEnfantDetailDto
                {
                    TeacherId = teacherEnfant.TeacherId,
                    EnfantId = teacherEnfant.EnfantId,
                    TeacherNom = teacherEnfant.Teacher.NomComplet,
                    TeacherEmail = teacherEnfant.Teacher.Email,
                    EnfantNom = $"{teacherEnfant.Enfant.Prenom} {teacherEnfant.Enfant.Nom}",
                    EnfantDateNaissance = teacherEnfant.Enfant.DateNaissance,
                    EnfantSexe = teacherEnfant.Enfant.Sexe,
                    ClasseNom = teacherEnfant.Enfant.Classe?.Nom,
                    ClasseId = teacherEnfant.Enfant.ClasseId,
                    AnneeScolaire = teacherEnfant.Enfant.AnneeScolaire,
                    EcoleNom = teacherEnfant.Ecole.Nom,
                    CreatedAt = teacherEnfant.CreatedAt
                };

                return CreatedAtAction(nameof(GetTeacherEnfant),
                    new { ecoleId, teacherId = teacherEnfant.TeacherId, enfantId = teacherEnfant.EnfantId },
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la relation teacher-enfant pour l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la relation");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/teacher-enfants/{teacherId}/{enfantId}
        [HttpDelete("{teacherId}/{enfantId:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteTeacherEnfant(int ecoleId, string teacherId, int enfantId)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(teacherId))
                {
                    return BadRequest("L'identifiant de l'enseignant est requis");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var teacherEnfant = await _context.TeacherEnfants
                    .FirstOrDefaultAsync(te => te.TeacherId == teacherId &&
                                              te.EnfantId == enfantId &&
                                              te.EcoleId == ecoleId &&
                                              !te.IsDeleted);

                if (teacherEnfant == null)
                {
                    return NotFound("Relation teacher-enfant non trouvée");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                teacherEnfant.IsDeleted = true;
                teacherEnfant.UpdatedAt = DateTime.UtcNow;
                teacherEnfant.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Relation teacher-enfant supprimée: {TeacherId} -> {EnfantId} dans l'école {EcoleId}",
                    teacherId, enfantId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la relation teacher-enfant {TeacherId}-{EnfantId} de l'école {EcoleId}",
                    teacherId, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la relation");
            }
        }

        // GET: api/ecoles/{ecoleId}/teacher-enfants/enseignants/{teacherId}/eleves
        [HttpGet("enseignants/{teacherId}/eleves")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<EnfantTeacherDto>>> GetElevesByTeacher(int ecoleId, string teacherId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessTeacherData(ecoleId, teacherId))
                {
                    return Forbid("Accès non autorisé aux données de cet enseignant");
                }

                var eleves = await _context.TeacherEnfants
                    .Where(te => te.TeacherId == teacherId &&
                                te.EcoleId == ecoleId &&
                                !te.IsDeleted)
                    .Include(te => te.Enfant)
                        .ThenInclude(e => e.Classe)
                    .Select(te => new EnfantTeacherDto
                    {
                        Id = te.Enfant.Id,
                        Nom = te.Enfant.Nom,
                        Prenom = te.Enfant.Prenom,
                        DateNaissance = te.Enfant.DateNaissance,
                        Sexe = te.Enfant.Sexe,
                        ClasseNom = te.Enfant.Classe != null ? te.Enfant.Classe.Nom : "Non assigné",
                        ClasseId = te.Enfant.ClasseId,
                        AnneeScolaire = te.Enfant.AnneeScolaire
                    })
                    .OrderBy(e => e.Nom)
                    .ThenBy(e => e.Prenom)
                    .ToListAsync();

                return Ok(eleves);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des élèves de l'enseignant {TeacherId} de l'école {EcoleId}",
                    teacherId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des élèves");
            }
        }

        // GET: api/ecoles/{ecoleId}/teacher-enfants/enfants/{enfantId}/enseignants
        [HttpGet("enfants/{enfantId:int}/enseignants")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<TeacherSimpleDto>>> GetTeachersByEnfant(int ecoleId, int enfantId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEnfantData(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé aux données de cet enfant");
                }

                var teachers = await _context.TeacherEnfants
                    .Where(te => te.EnfantId == enfantId &&
                                te.EcoleId == ecoleId &&
                                !te.IsDeleted)
                    .Include(te => te.Teacher)
                    .Select(te => new TeacherSimpleDto
                    {
                        Id = te.Teacher.Id,
                        Nom = te.Teacher.Nom,
                        Prenom = te.Teacher.Prenom,
                        Email = te.Teacher.Email,
                        Telephone = te.Teacher.Telephone
                    })
                    .OrderBy(t => t.Nom)
                    .ThenBy(t => t.Prenom)
                    .ToListAsync();

                return Ok(teachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des enseignants de l'enfant {EnfantId} de l'école {EcoleId}",
                    enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des enseignants");
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

        private async Task<bool> CanAccessTeacherEnfant(int ecoleId, string teacherId, int enfantId)
        {
            if (!await CanAccessEcole(ecoleId))
                return false;

            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
                return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Teacher"))
            {
                return currentUserId == teacherId;
            }

            return false;
        }

        private async Task<bool> CanAccessTeacherData(int ecoleId, string teacherId)
        {
            if (!await CanAccessEcole(ecoleId))
                return false;

            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
                return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Teacher"))
            {
                return currentUserId == teacherId;
            }

            return false;
        }

        private async Task<bool> CanAccessEnfantData(int ecoleId, int enfantId)
        {
            if (!await CanAccessEcole(ecoleId))
                return false;

            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
                return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Teacher"))
            {
                // Vérifier si l'enseignant a accès à cet enfant
                return await _context.TeacherEnfants
                    .AnyAsync(te => te.TeacherId == currentUserId &&
                                   te.EnfantId == enfantId &&
                                   !te.IsDeleted);
            }

            if (User.IsInRole("Parent"))
            {
                // Vérifier si le parent a accès à cet enfant
                return await _context.ParentEnfants
                    .AnyAsync(pe => pe.ParentId == currentUserId &&
                                   pe.EnfantId == enfantId &&
                                   !pe.IsDeleted);
            }

            return false;
        }
    }

    // DTOs pour TeacherEnfant
    public class TeacherEnfantListeDto
    {
        public string TeacherId { get; set; } = string.Empty;
        public int EnfantId { get; set; }
        public string TeacherNom { get; set; } = string.Empty;
        public string EnfantNom { get; set; } = string.Empty;
        public string ClasseNom { get; set; } = string.Empty;
        public int? ClasseId { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TeacherEnfantDetailDto : TeacherEnfantListeDto
    {
        public string TeacherEmail { get; set; } = string.Empty;
        public DateTime EnfantDateNaissance { get; set; }
        public string EnfantSexe { get; set; } = string.Empty;
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateTeacherEnfantRequest
    {
        [Required(ErrorMessage = "L'identifiant de l'enseignant est obligatoire")]
        public string TeacherId { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'identifiant de l'enfant est obligatoire")]
        public int EnfantId { get; set; }
    }



    public class EnfantTeacherDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public string ClasseNom { get; set; } = string.Empty;
        public int? ClasseId { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty;
    }

    public class TeacherSimpleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
    }
}