using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using OfficeOpenXml;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/classes/{classeId}/emplois")]
    [ApiController]
    [Authorize]
    public class EmploiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmploiController> _logger;

        public EmploiController(
            ApplicationDbContext context,
            ILogger<EmploiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/classes/{classeId}/emplois
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<EmploiListeDto>>> GetEmplois(
            int ecoleId,
            int classeId,
            [FromQuery] string? anneeScolaire = null,
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

                if (classeId <= 0)
                {
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClasse(ecoleId, classeId))
                {
                    return Forbid("Accès non autorisé à cette classe");
                }

                // Vérifier que la classe existe
                var classe = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Id == classeId && c.EcoleId == ecoleId && !c.IsDeleted);

                if (classe == null)
                {
                    return NotFound($"Classe avec l'ID {classeId} non trouvée dans l'école {ecoleId}");
                }

                var query = _context.Emplois
                    .Where(e => e.ClasseId == classeId && !e.IsDeleted);

                // Filtrage par année scolaire
                if (!string.IsNullOrEmpty(anneeScolaire))
                {
                    query = query.Where(e => e.AnneeScolaire == anneeScolaire);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var emplois = await query
                    .Include(e => e.Classe)
                        .ThenInclude(c => c.EnseignantPrincipal)
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new EmploiListeDto
                    {
                        Id = e.Id,
                        NomFichier = e.NomFichier,
                        AnneeScolaire = e.AnneeScolaire,
                        TailleFichier = e.FichierEmploi.Length,
                        ClasseId = e.ClasseId,
                        ClasseNom = e.Classe.Nom,
                        EnseignantPrincipalNom = e.Classe.EnseignantPrincipal != null ? e.Classe.EnseignantPrincipal.NomComplet : null,
                        CreatedAt = e.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(emplois);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des emplois du temps de la classe {ClasseId} de l'école {EcoleId}", classeId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des emplois du temps");
            }
        }

        // GET: api/ecoles/{ecoleId}/classes/{classeId}/emplois/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<EmploiDetailDto>> GetEmploi(int ecoleId, int classeId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (classeId <= 0)
                {
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'emploi du temps est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClasse(ecoleId, classeId))
                {
                    return Forbid("Accès non autorisé à cette classe");
                }

                var emploi = await _context.Emplois
                    .Include(e => e.Classe)
                        .ThenInclude(c => c.EnseignantPrincipal)
                    .Include(e => e.Classe.Ecole)
                    .Where(e => e.Id == id && e.ClasseId == classeId && !e.IsDeleted)
                    .FirstOrDefaultAsync();

                if (emploi == null)
                {
                    return NotFound($"Emploi du temps avec l'ID {id} non trouvé pour la classe {classeId}");
                }

                var emploiDto = new EmploiDetailDto
                {
                    Id = emploi.Id,
                    NomFichier = emploi.NomFichier,
                    AnneeScolaire = emploi.AnneeScolaire,
                    TailleFichier = emploi.FichierEmploi.Length,
                    ClasseId = emploi.ClasseId,
                    ClasseNom = emploi.Classe.Nom,
                    EnseignantPrincipalNom = emploi.Classe.EnseignantPrincipal?.NomComplet,
                    EcoleNom = emploi.Classe.Ecole?.Nom ?? string.Empty,
                    CreatedAt = emploi.CreatedAt,
                    UpdatedAt = emploi.UpdatedAt
                };

                return Ok(emploiDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'emploi du temps {EmploiId} de la classe {ClasseId} de l'école {EcoleId}", id, classeId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'emploi du temps");
            }
        }

        // POST: api/ecoles/{ecoleId}/classes/{classeId}/emplois
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<EmploiDetailDto>> CreateEmploi(int ecoleId, int classeId, [FromForm] CreateEmploiRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (classeId <= 0)
                {
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClasse(ecoleId, classeId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette classe");
                }

                // Vérifier que la classe existe
                var classe = await _context.Classes
                    .Include(c => c.Ecole)
                    .FirstOrDefaultAsync(c => c.Id == classeId && c.EcoleId == ecoleId && !c.IsDeleted);

                if (classe == null)
                {
                    return NotFound($"Classe avec l'ID {classeId} non trouvée dans l'école {ecoleId}");
                }

                // Valider et lire le fichier
                if (request.Fichier == null || request.Fichier.Length == 0)
                {
                    return BadRequest("Le fichier est obligatoire");
                }

                // Limite de taille (par exemple 10MB)
                const long maxFileSize = 10 * 1024 * 1024;
                if (request.Fichier.Length > maxFileSize)
                {
                    return BadRequest($"Le fichier est trop volumineux. Taille maximale autorisée : {maxFileSize / (1024 * 1024)}MB");
                }

                // Vérifier l'extension du fichier
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.Fichier.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest($"Type de fichier non autorisé. Extensions autorisées : {string.Join(", ", allowedExtensions)}");
                }

                // Vérifier l'unicité du nom de fichier pour cette classe et année scolaire
                var existingEmploi = await _context.Emplois
                    .AnyAsync(e => e.ClasseId == classeId &&
                                 e.AnneeScolaire == request.AnneeScolaire &&
                                 e.NomFichier.ToLower() == request.NomFichier.ToLower() &&
                                 !e.IsDeleted);

                if (existingEmploi)
                {
                    return BadRequest($"Un emploi du temps avec le nom '{request.NomFichier}' existe déjà pour cette classe et cette année scolaire");
                }

                byte[] fichierBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await request.Fichier.CopyToAsync(memoryStream);
                    fichierBytes = memoryStream.ToArray();
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var emploi = new Emploi
                {
                    ClasseId = classeId,
                    NomFichier = request.NomFichier,
                    FichierEmploi = fichierBytes,
                    AnneeScolaire = request.AnneeScolaire,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId
                };

                _context.Emplois.Add(emploi);
                await _context.SaveChangesAsync();

                // Recharger l'emploi avec les relations
                emploi = await _context.Emplois
                    .Include(e => e.Classe)
                        .ThenInclude(c => c.EnseignantPrincipal)
                    .Include(e => e.Classe.Ecole)
                    .FirstAsync(e => e.Id == emploi.Id);

                _logger.LogInformation("Emploi du temps '{NomFichier}' créé pour la classe {ClasseId} de l'école {EcoleId} avec l'ID {Id}",
                    emploi.NomFichier, classeId, ecoleId, emploi.Id);

                var result = new EmploiDetailDto
                {
                    Id = emploi.Id,
                    NomFichier = emploi.NomFichier,
                    AnneeScolaire = emploi.AnneeScolaire,
                    TailleFichier = emploi.FichierEmploi.Length,
                    ClasseId = emploi.ClasseId,
                    ClasseNom = emploi.Classe.Nom,
                    EnseignantPrincipalNom = emploi.Classe.EnseignantPrincipal?.NomComplet,
                    EcoleNom = emploi.Classe.Ecole?.Nom ?? string.Empty,
                    CreatedAt = emploi.CreatedAt
                };

                return CreatedAtAction(nameof(GetEmploi), new { ecoleId, classeId, id = emploi.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'emploi du temps pour la classe {ClasseId} de l'école {EcoleId}", classeId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'emploi du temps");
            }
        }

        // PUT: api/ecoles/{ecoleId}/classes/{classeId}/emplois/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<IActionResult> UpdateEmploi(int ecoleId, int classeId, int id, [FromForm] UpdateEmploiRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (classeId <= 0)
                {
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'emploi du temps est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClasse(ecoleId, classeId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette classe");
                }

                var emploi = await _context.Emplois
                    .FirstOrDefaultAsync(e => e.Id == id && e.ClasseId == classeId && !e.IsDeleted);

                if (emploi == null)
                {
                    return NotFound($"Emploi du temps avec l'ID {id} non trouvé pour la classe {classeId}");
                }

                // Vérifier l'unicité du nom si modifié
                if (!string.IsNullOrEmpty(request.NomFichier) &&
                    request.NomFichier.ToLower() != emploi.NomFichier.ToLower())
                {
                    var existingNom = await _context.Emplois
                        .AnyAsync(e => e.Id != id &&
                                     e.ClasseId == classeId &&
                                     e.AnneeScolaire == (request.AnneeScolaire ?? emploi.AnneeScolaire) &&
                                     e.NomFichier.ToLower() == request.NomFichier.ToLower() &&
                                     !e.IsDeleted);

                    if (existingNom)
                    {
                        return BadRequest($"Un emploi du temps avec le nom '{request.NomFichier}' existe déjà pour cette classe et cette année scolaire");
                    }
                }

                // Traiter le nouveau fichier si fourni
                if (request.Fichier != null && request.Fichier.Length > 0)
                {
                    // Limite de taille (par exemple 10MB)
                    const long maxFileSize = 10 * 1024 * 1024;
                    if (request.Fichier.Length > maxFileSize)
                    {
                        return BadRequest($"Le fichier est trop volumineux. Taille maximale autorisée : {maxFileSize / (1024 * 1024)}MB");
                    }

                    // Vérifier l'extension du fichier
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(request.Fichier.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest($"Type de fichier non autorisé. Extensions autorisées : {string.Join(", ", allowedExtensions)}");
                    }

                    byte[] fichierBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await request.Fichier.CopyToAsync(memoryStream);
                        fichierBytes = memoryStream.ToArray();
                    }

                    emploi.FichierEmploi = fichierBytes;
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.NomFichier))
                    emploi.NomFichier = request.NomFichier;
                if (!string.IsNullOrEmpty(request.AnneeScolaire))
                    emploi.AnneeScolaire = request.AnneeScolaire;

                emploi.UpdatedAt = DateTime.UtcNow;
                emploi.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Emploi du temps {Id} mis à jour pour la classe {ClasseId} de l'école {EcoleId}", id, classeId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'emploi du temps {EmploiId} de la classe {ClasseId} de l'école {EcoleId}", id, classeId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'emploi du temps");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/classes/{classeId}/emplois/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<IActionResult> DeleteEmploi(int ecoleId, int classeId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (classeId <= 0)
                {
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'emploi du temps est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClasse(ecoleId, classeId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette classe");
                }

                var emploi = await _context.Emplois
                    .FirstOrDefaultAsync(e => e.Id == id && e.ClasseId == classeId && !e.IsDeleted);

                if (emploi == null)
                {
                    return NotFound($"Emploi du temps avec l'ID {id} non trouvé pour la classe {classeId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                emploi.IsDeleted = true;
                emploi.UpdatedAt = DateTime.UtcNow;
                emploi.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Emploi du temps '{NomFichier}' supprimé (soft delete) de la classe {ClasseId} de l'école {EcoleId}",
                    emploi.NomFichier, classeId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'emploi du temps {EmploiId} de la classe {ClasseId} de l'école {EcoleId}", id, classeId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'emploi du temps");
            }
        }

        // GET: api/ecoles/{ecoleId}/classes/{classeId}/emplois/{id}/telecharger
        [HttpGet("{id:int}/telecharger")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<IActionResult> TelechargerEmploi(int ecoleId, int classeId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (classeId <= 0)
                {
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'emploi du temps est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClasse(ecoleId, classeId))
                {
                    return Forbid("Accès non autorisé à cette classe");
                }

                var emploi = await _context.Emplois
                    .FirstOrDefaultAsync(e => e.Id == id && e.ClasseId == classeId && !e.IsDeleted);

                if (emploi == null)
                {
                    return NotFound($"Emploi du temps avec l'ID {id} non trouvé pour la classe {classeId}");
                }

                // Déterminer le type MIME basé sur l'extension du fichier
                var contentType = GetContentType(emploi.NomFichier);

                return File(emploi.FichierEmploi, contentType, emploi.NomFichier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement de l'emploi du temps {EmploiId} de la classe {ClasseId} de l'école {EcoleId}", id, classeId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors du téléchargement de l'emploi du temps");
            }
        }

        // GET: api/ecoles/{ecoleId}/classes/{classeId}/emplois/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<EmploiStatistiquesDto>> GetStatistiques(int ecoleId, int classeId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessClasse(ecoleId, classeId))
                {
                    return Forbid("Accès non autorisé à cette classe");
                }

                var emplois = await _context.Emplois
                    .Where(e => e.ClasseId == classeId && !e.IsDeleted)
                    .ToListAsync();

                var classe = await _context.Classes
                    .Include(c => c.EnseignantPrincipal)
                    .FirstOrDefaultAsync(c => c.Id == classeId);

                var statistiques = new EmploiStatistiquesDto
                {
                    ClasseId = classeId,
                    ClasseNom = classe?.Nom ?? string.Empty,
                    EnseignantPrincipalNom = classe?.EnseignantPrincipal?.NomComplet,
                    NombreTotalEmplois = emplois.Count,
                    TailleTotaleEmplois = emplois.Sum(e => (long)e.FichierEmploi.Length),
                    AnnesScolaires = emplois.GroupBy(e => e.AnneeScolaire)
                        .Select(g => new AnneeScolaireStatDto
                        {
                            AnneeScolaire = g.Key,
                            NombreEmplois = g.Count(),
                            TailleTotal = g.Sum(e => (long)e.FichierEmploi.Length)
                        })
                        .OrderByDescending(a => a.AnneeScolaire)
                        .ToList()
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des emplois du temps de la classe {ClasseId} de l'école {EcoleId}", classeId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }



        // Méthodes d'aide
        private async Task<bool> CanAccessClasse(int ecoleId, int classeId)
        {
            if (User.IsInRole("SuperAdmin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.EcoleId != ecoleId)
                return false;

            if (User.IsInRole("Admin") || User.IsInRole("Teacher"))
                return true;

            if (User.IsInRole("Parent"))
            {
                // Vérifier si le parent a un enfant dans cette classe
                var hasChildInClass = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                    .AnyAsync(e => e.ClasseId == classeId && !e.IsDeleted && e.ParentsEnfants.Any(pe => pe.ParentId == userId));
                return hasChildInClass;
            }

            return false;
        }

        private async Task<bool> CanManageClasse(int ecoleId, int classeId)
        {
            if (User.IsInRole("SuperAdmin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.EcoleId != ecoleId)
                return false;

            if (User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("Teacher"))
            {
                // Vérifier si l'enseignant est le principal de cette classe
                var isTeacherOfClass = await _context.Classes
                    .AnyAsync(c => c.Id == classeId && c.EnseignantPrincipalId == userId);
                return isTeacherOfClass;
            }

            return false;
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }

    // DTOs pour les emplois du temps
    public class EmploiListeDto
    {
        public int Id { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public long TailleFichier { get; set; }
        public int ClasseId { get; set; }
        public string ClasseNom { get; set; } = string.Empty;
        public string? EnseignantPrincipalNom { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EmploiDetailDto : EmploiListeDto
    {
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateEmploiRequest
    {
        [Required(ErrorMessage = "Le nom du fichier est obligatoire")]
        public string NomFichier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le fichier est obligatoire")]
        public IFormFile Fichier { get; set; } = null!;

        [Required(ErrorMessage = "L'année scolaire est obligatoire")]
        public string AnneeScolaire { get; set; } = string.Empty;
    }

    public class UpdateEmploiRequest
    {
        public string? NomFichier { get; set; }
        public IFormFile? Fichier { get; set; }
        public string? AnneeScolaire { get; set; }
    }

    public class EmploiStatistiquesDto
    {
        public int ClasseId { get; set; }
        public string ClasseNom { get; set; } = string.Empty;
        public string? EnseignantPrincipalNom { get; set; }
        public int NombreTotalEmplois { get; set; }
        public long TailleTotaleEmplois { get; set; }
        public List<AnneeScolaireStatDto> AnnesScolaires { get; set; } = new List<AnneeScolaireStatDto>();
    }

    public class AnneeScolaireStatDto
    {
        public string AnneeScolaire { get; set; } = string.Empty;
        public int NombreEmplois { get; set; }
        public long TailleTotal { get; set; }
    }
}