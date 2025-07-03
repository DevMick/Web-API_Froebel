using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/parent-enfants")]
    [ApiController]
    [Authorize]
    public class ParentEnfantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ParentEnfantController> _logger;

        public ParentEnfantController(
            ApplicationDbContext context,
            ILogger<ParentEnfantController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/parent-enfants
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<ParentEnfantListeDto>>> GetParentEnfants(
            int ecoleId,
            [FromQuery] string? parentId = null,
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

                var query = _context.ParentEnfants
                    .Where(pe => pe.EcoleId == ecoleId && !pe.IsDeleted);

                // Si c'est un parent, ne montrer que ses enfants
                if (User.IsInRole("Parent") && !User.IsInRole("Admin"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    query = query.Where(pe => pe.ParentId == currentUserId);
                }

                // Si c'est un enseignant, ne montrer que les enfants de ses classes
                if (User.IsInRole("Teacher") && !User.IsInRole("Admin"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var enfantsAccessibles = _context.TeacherEnfants
                        .Where(te => te.TeacherId == currentUserId && !te.IsDeleted)
                        .Select(te => te.EnfantId);

                    query = query.Where(pe => enfantsAccessibles.Contains(pe.EnfantId));
                }

                // Filtrage par parent
                if (!string.IsNullOrEmpty(parentId))
                {
                    query = query.Where(pe => pe.ParentId == parentId);
                }

                // Filtrage par enfant
                if (enfantId.HasValue)
                {
                    query = query.Where(pe => pe.EnfantId == enfantId.Value);
                }

                // Filtrage par classe
                if (classeId.HasValue)
                {
                    query = query.Where(pe => pe.Enfant.ClasseId == classeId.Value);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(pe =>
                        pe.Enfant.Nom.ToLower().Contains(termeLower) ||
                        pe.Enfant.Prenom.ToLower().Contains(termeLower) ||
                        pe.Parent.Nom.ToLower().Contains(termeLower) ||
                        pe.Parent.Prenom.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var parentEnfants = await query
                    .Include(pe => pe.Parent)
                    .Include(pe => pe.Enfant)
                        .ThenInclude(e => e.Classe)
                    .OrderBy(pe => pe.Parent.Nom)
                        .ThenBy(pe => pe.Parent.Prenom)
                        .ThenBy(pe => pe.Enfant.Nom)
                        .ThenBy(pe => pe.Enfant.Prenom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(pe => new ParentEnfantListeDto
                    {
                        ParentId = pe.ParentId,
                        EnfantId = pe.EnfantId,
                        ParentNom = pe.Parent.NomComplet,
                        ParentEmail = pe.Parent.Email,
                        ParentTelephone = pe.Parent.Telephone,
                        EnfantNom = $"{pe.Enfant.Prenom} {pe.Enfant.Nom}",
                        EnfantDateNaissance = pe.Enfant.DateNaissance,
                        ClasseNom = pe.Enfant.Classe != null ? pe.Enfant.Classe.Nom : "Non assigné",
                        ClasseId = pe.Enfant.ClasseId,
                        AnneeScolaire = pe.Enfant.AnneeScolaire,
                        CreatedAt = pe.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(parentEnfants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des relations parent-enfant de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des relations parent-enfant");
            }
        }

        // GET: api/ecoles/{ecoleId}/parent-enfants/{parentId}/{enfantId}
        [HttpGet("{parentId}/{enfantId:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<ParentEnfantDetailDto>> GetParentEnfant(int ecoleId, string parentId, int enfantId)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(parentId))
                {
                    return BadRequest("L'identifiant du parent est requis");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessParentEnfant(ecoleId, parentId, enfantId))
                {
                    return Forbid("Accès non autorisé à cette relation");
                }

                var parentEnfant = await _context.ParentEnfants
                    .Include(pe => pe.Parent)
                    .Include(pe => pe.Enfant)
                        .ThenInclude(e => e.Classe)
                    .Include(pe => pe.Ecole)
                    .Where(pe => pe.ParentId == parentId &&
                                pe.EnfantId == enfantId &&
                                pe.EcoleId == ecoleId &&
                                !pe.IsDeleted)
                    .FirstOrDefaultAsync();

                if (parentEnfant == null)
                {
                    return NotFound($"Relation parent-enfant non trouvée");
                }

                var result = new ParentEnfantDetailDto
                {
                    ParentId = parentEnfant.ParentId,
                    EnfantId = parentEnfant.EnfantId,
                    ParentNom = parentEnfant.Parent.NomComplet,
                    ParentEmail = parentEnfant.Parent.Email,
                    ParentTelephone = parentEnfant.Parent.Telephone,
                    ParentAdresse = parentEnfant.Parent.Adresse,
                    EnfantNom = $"{parentEnfant.Enfant.Prenom} {parentEnfant.Enfant.Nom}",
                    EnfantDateNaissance = parentEnfant.Enfant.DateNaissance,
                    EnfantSexe = parentEnfant.Enfant.Sexe,
                    EnfantStatut = parentEnfant.Enfant.Statut,
                    EnfantUtiliseCantine = parentEnfant.Enfant.UtiliseCantine,
                    ClasseNom = parentEnfant.Enfant.Classe?.Nom,
                    ClasseId = parentEnfant.Enfant.ClasseId,
                    AnneeScolaire = parentEnfant.Enfant.AnneeScolaire,
                    EcoleNom = parentEnfant.Ecole.Nom,
                    CreatedAt = parentEnfant.CreatedAt,
                    UpdatedAt = parentEnfant.UpdatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la relation parent-enfant {ParentId}-{EnfantId} de l'école {EcoleId}",
                    parentId, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la relation");
            }
        }

        // POST: api/ecoles/{ecoleId}/parent-enfants
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ParentEnfantDetailDto>> CreateParentEnfant(int ecoleId, [FromBody] CreateParentEnfantRequest request)
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

                // Vérifier que le parent existe et appartient à cette école
                var parent = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.ParentId &&
                                             u.EcoleId == ecoleId);

                if (parent == null)
                {
                    return BadRequest("Parent non trouvé dans cette école");
                }

                // Vérifier que c'est bien un parent
                var isParent = await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .AnyAsync(ur => ur.UserId == request.ParentId && ur.Name == "Parent");

                if (!isParent)
                {
                    return BadRequest("L'utilisateur spécifié n'est pas un parent");
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
                var existingRelation = await _context.ParentEnfants
                    .FirstOrDefaultAsync(pe => pe.ParentId == request.ParentId &&
                                              pe.EnfantId == request.EnfantId &&
                                              !pe.IsDeleted);

                if (existingRelation != null)
                {
                    return BadRequest("Cette relation parent-enfant existe déjà");
                }

                var parentEnfant = new ParentEnfant
                {
                    ParentId = request.ParentId,
                    EnfantId = request.EnfantId,
                    EcoleId = ecoleId
                };

                _context.ParentEnfants.Add(parentEnfant);
                await _context.SaveChangesAsync();

                // Recharger avec les relations
                parentEnfant = await _context.ParentEnfants
                    .Include(pe => pe.Parent)
                    .Include(pe => pe.Enfant)
                        .ThenInclude(e => e.Classe)
                    .Include(pe => pe.Ecole)
                    .FirstAsync(pe => pe.ParentId == parentEnfant.ParentId &&
                                     pe.EnfantId == parentEnfant.EnfantId);

                _logger.LogInformation("Relation parent-enfant créée: {ParentId} -> {EnfantId} dans l'école {EcoleId}",
                    request.ParentId, request.EnfantId, ecoleId);

                var result = new ParentEnfantDetailDto
                {
                    ParentId = parentEnfant.ParentId,
                    EnfantId = parentEnfant.EnfantId,
                    ParentNom = parentEnfant.Parent.NomComplet,
                    ParentEmail = parentEnfant.Parent.Email,
                    ParentTelephone = parentEnfant.Parent.Telephone,
                    ParentAdresse = parentEnfant.Parent.Adresse,
                    EnfantNom = $"{parentEnfant.Enfant.Prenom} {parentEnfant.Enfant.Nom}",
                    EnfantDateNaissance = parentEnfant.Enfant.DateNaissance,
                    EnfantSexe = parentEnfant.Enfant.Sexe,
                    EnfantStatut = parentEnfant.Enfant.Statut,
                    EnfantUtiliseCantine = parentEnfant.Enfant.UtiliseCantine,
                    ClasseNom = parentEnfant.Enfant.Classe?.Nom,
                    ClasseId = parentEnfant.Enfant.ClasseId,
                    AnneeScolaire = parentEnfant.Enfant.AnneeScolaire,
                    EcoleNom = parentEnfant.Ecole.Nom,
                    CreatedAt = parentEnfant.CreatedAt
                };

                return CreatedAtAction(nameof(GetParentEnfant),
                    new { ecoleId, parentId = parentEnfant.ParentId, enfantId = parentEnfant.EnfantId },
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la relation parent-enfant pour l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la relation");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/parent-enfants/{parentId}/{enfantId}
        [HttpDelete("{parentId}/{enfantId:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteParentEnfant(int ecoleId, string parentId, int enfantId)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (string.IsNullOrEmpty(parentId))
                {
                    return BadRequest("L'identifiant du parent est requis");
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

                var parentEnfant = await _context.ParentEnfants
                    .FirstOrDefaultAsync(pe => pe.ParentId == parentId &&
                                              pe.EnfantId == enfantId &&
                                              pe.EcoleId == ecoleId &&
                                              !pe.IsDeleted);

                if (parentEnfant == null)
                {
                    return NotFound("Relation parent-enfant non trouvée");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                parentEnfant.IsDeleted = true;
                parentEnfant.UpdatedAt = DateTime.UtcNow;
                parentEnfant.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Relation parent-enfant supprimée: {ParentId} -> {EnfantId} dans l'école {EcoleId}",
                    parentId, enfantId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la relation parent-enfant {ParentId}-{EnfantId} de l'école {EcoleId}",
                    parentId, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la relation");
            }
        }

        // GET: api/ecoles/{ecoleId}/parent-enfants/parents/{parentId}/enfants
        [HttpGet("parents/{parentId}/enfants")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<EnfantParentDto>>> GetEnfantsByParent(int ecoleId, string parentId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessParentData(ecoleId, parentId))
                {
                    return Forbid("Accès non autorisé aux données de ce parent");
                }

                var enfants = await _context.ParentEnfants
                    .Where(pe => pe.ParentId == parentId &&
                                pe.EcoleId == ecoleId &&
                                !pe.IsDeleted)
                    .Include(pe => pe.Enfant)
                        .ThenInclude(e => e.Classe)
                    .Select(pe => new EnfantParentDto
                    {
                        Id = pe.Enfant.Id,
                        Nom = pe.Enfant.Nom,
                        Prenom = pe.Enfant.Prenom,
                        DateNaissance = pe.Enfant.DateNaissance,
                        Sexe = pe.Enfant.Sexe,
                        Statut = pe.Enfant.Statut,
                        ClasseNom = pe.Enfant.Classe != null ? pe.Enfant.Classe.Nom : "Non assigné",
                        ClasseId = pe.Enfant.ClasseId,
                        AnneeScolaire = pe.Enfant.AnneeScolaire,
                        UtiliseCantine = pe.Enfant.UtiliseCantine,
                        DateInscription = pe.Enfant.DateInscription
                    })
                    .OrderBy(e => e.Nom)
                    .ThenBy(e => e.Prenom)
                    .ToListAsync();

                return Ok(enfants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des enfants du parent {ParentId} de l'école {EcoleId}",
                    parentId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des enfants");
            }
        }

        // GET: api/ecoles/{ecoleId}/parent-enfants/enfants/{enfantId}/parents
        [HttpGet("enfants/{enfantId:int}/parents")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<ParentDetailDto>>> GetParentsByEnfant(int ecoleId, int enfantId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEnfantData(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé aux données de cet enfant");
                }

                var parents = await _context.ParentEnfants
                    .Where(pe => pe.EnfantId == enfantId &&
                                pe.EcoleId == ecoleId &&
                                !pe.IsDeleted)
                    .Include(pe => pe.Parent)
                    .Select(pe => new ParentDetailDto
                    {
                        Id = pe.Parent.Id,
                        Nom = pe.Parent.Nom,
                        Prenom = pe.Parent.Prenom,
                        Email = pe.Parent.Email,
                        Telephone = pe.Parent.Telephone,
                        Adresse = pe.Parent.Adresse,
                        Sexe = pe.Parent.Sexe,
                        DateNaissance = pe.Parent.DateNaissance
                    })
                    .OrderBy(p => p.Nom)
                    .ThenBy(p => p.Prenom)
                    .ToListAsync();

                return Ok(parents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des parents de l'enfant {EnfantId} de l'école {EcoleId}",
                    enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des parents");
            }
        }

        // GET: api/ecoles/{ecoleId}/parent-enfants/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ParentEnfantStatistiquesDto>> GetStatistiques(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var relations = await _context.ParentEnfants
                    .Where(pe => pe.EcoleId == ecoleId && !pe.IsDeleted)
                    .Include(pe => pe.Enfant)
                    .ToListAsync();

                var ecole = await _context.Ecoles.FindAsync(ecoleId);

                var statistiques = new ParentEnfantStatistiquesDto
                {
                    EcoleId = ecoleId,
                    EcoleNom = ecole?.Nom ?? string.Empty,
                    NombreTotalRelations = relations.Count,
                    NombreParentsUniques = relations.Select(r => r.ParentId).Distinct().Count(),
                    NombreEnfantsUniques = relations.Select(r => r.EnfantId).Distinct().Count(),
                    NombreEnfantsAvecPlusieursParents = relations
                        .GroupBy(r => r.EnfantId)
                        .Count(g => g.Count() > 1),
                    NombreParentsAvecPlusieursEnfants = relations
                        .GroupBy(r => r.ParentId)
                        .Count(g => g.Count() > 1),
                    EnfantsParClasse = relations
                        .Where(r => r.Enfant.Classe != null)
                        .GroupBy(r => r.Enfant.Classe!.Nom)
                        .Select(g => new ClasseStatDto
                        {
                            ClasseNom = g.Key,
                            NombreEnfants = g.Select(r => r.EnfantId).Distinct().Count()
                        })
                        .OrderBy(c => c.ClasseNom)
                        .ToList()
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des relations parent-enfant de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
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

        private async Task<bool> CanAccessParentEnfant(int ecoleId, string parentId, int enfantId)
        {
            if (!await CanAccessEcole(ecoleId))
                return false;

            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
                return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Parent"))
            {
                return currentUserId == parentId;
            }

            if (User.IsInRole("Teacher"))
            {
                // Vérifier si l'enseignant a accès à cet enfant
                return await _context.TeacherEnfants
                    .AnyAsync(te => te.TeacherId == currentUserId &&
                                   te.EnfantId == enfantId &&
                                   !te.IsDeleted);
            }

            return false;
        }

        private async Task<bool> CanAccessParentData(int ecoleId, string parentId)
        {
            if (!await CanAccessEcole(ecoleId))
                return false;

            if (User.IsInRole("SuperAdmin") || User.IsInRole("Admin"))
                return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Parent"))
            {
                return currentUserId == parentId;
            }

            if (User.IsInRole("Teacher"))
            {
                // Vérifier si l'enseignant a accès aux enfants de ce parent
                var enfantsDuParent = await _context.ParentEnfants
                    .Where(pe => pe.ParentId == parentId && !pe.IsDeleted)
                    .Select(pe => pe.EnfantId)
                    .ToListAsync();

                return await _context.TeacherEnfants
                    .AnyAsync(te => te.TeacherId == currentUserId &&
                                   enfantsDuParent.Contains(te.EnfantId) &&
                                   !te.IsDeleted);
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

    // DTOs pour ParentEnfant
    public class ParentEnfantListeDto
    {
        public string ParentId { get; set; } = string.Empty;
        public int EnfantId { get; set; }
        public string ParentNom { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string? ParentTelephone { get; set; }
        public string EnfantNom { get; set; } = string.Empty;
        public DateTime EnfantDateNaissance { get; set; }
        public string ClasseNom { get; set; } = string.Empty;
        public int? ClasseId { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ParentEnfantDetailDto : ParentEnfantListeDto
    {
        public string? ParentAdresse { get; set; }
        public string EnfantSexe { get; set; } = string.Empty;
        public string EnfantStatut { get; set; } = string.Empty;
        public bool EnfantUtiliseCantine { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateParentEnfantRequest
    {
        [Required(ErrorMessage = "L'identifiant du parent est obligatoire")]
        public string ParentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'identifiant de l'enfant est obligatoire")]
        public int EnfantId { get; set; }
    }

    public class EnfantParentDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string ClasseNom { get; set; } = string.Empty;
        public int? ClasseId { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty;
        public bool UtiliseCantine { get; set; }
        public DateTime? DateInscription { get; set; }
    }

    public class ParentDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public DateTime? DateNaissance { get; set; }
    }

    public class ParentEnfantStatistiquesDto
    {
        public int EcoleId { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public int NombreTotalRelations { get; set; }
        public int NombreParentsUniques { get; set; }
        public int NombreEnfantsUniques { get; set; }
        public int NombreEnfantsAvecPlusieursParents { get; set; }
        public int NombreParentsAvecPlusieursEnfants { get; set; }
        public List<ClasseStatDto> EnfantsParClasse { get; set; } = new List<ClasseStatDto>();
    }

    public class ClasseStatDto
    {
        public string ClasseNom { get; set; } = string.Empty;
        public int NombreEnfants { get; set; }
    }
}