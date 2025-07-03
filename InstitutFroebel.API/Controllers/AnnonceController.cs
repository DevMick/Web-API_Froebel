using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/annonces")]
    [ApiController]
    [Authorize]
    public class AnnonceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnnonceController> _logger;

        public AnnonceController(
            ApplicationDbContext context,
            ILogger<AnnonceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/annonces
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<AnnonceListeDto>>> GetAnnonces(
            int ecoleId,
            [FromQuery] string? type = null,
            [FromQuery] string? classeCible = null,
            [FromQuery] bool? envoyerNotification = null,
            [FromQuery] string? createdById = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
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

                var query = _context.Annonces
                    .Where(a => a.EcoleId == ecoleId && !a.IsDeleted);

                // Filtrage par type
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(a => a.Type == type);
                }

                // Filtrage par classe cible
                if (!string.IsNullOrEmpty(classeCible))
                {
                    query = query.Where(a => a.ClasseCible == classeCible || a.ClasseCible == null);
                }

                // Filtrage par notification
                if (envoyerNotification.HasValue)
                {
                    query = query.Where(a => a.EnvoyerNotification == envoyerNotification.Value);
                }

                // Filtrage par créateur
                if (!string.IsNullOrEmpty(createdById))
                {
                    query = query.Where(a => a.CreatedById == createdById);
                }

                // Filtrage par période
                if (dateDebut.HasValue)
                {
                    query = query.Where(a => a.DatePublication >= dateDebut.Value);
                }
                if (dateFin.HasValue)
                {
                    query = query.Where(a => a.DatePublication <= dateFin.Value);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(a => a.Titre.ToLower().Contains(termeLower) ||
                                           a.Contenu.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var annonces = await query
                    .Include(a => a.CreatedBy)
                    .OrderByDescending(a => a.DatePublication)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new AnnonceListeDto
                    {
                        Id = a.Id,
                        Titre = a.Titre,
                        Contenu = a.Contenu.Length > 200 ? a.Contenu.Substring(0, 200) + "..." : a.Contenu,
                        Type = a.Type,
                        DatePublication = a.DatePublication,
                        ClasseCible = a.ClasseCible,
                        EnvoyerNotification = a.EnvoyerNotification,
                        CreatedById = a.CreatedById,
                        CreatedByNom = a.CreatedBy != null ? a.CreatedBy.NomComplet : "Système",
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(annonces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des annonces de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des annonces");
            }
        }

        // GET: api/ecoles/{ecoleId}/annonces/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<AnnonceDetailDto>> GetAnnonce(int ecoleId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'annonce est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var annonce = await _context.Annonces
                    .Include(a => a.CreatedBy)
                    .Include(a => a.Ecole)
                    .Where(a => a.Id == id && a.EcoleId == ecoleId && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                if (annonce == null)
                {
                    return NotFound($"Annonce avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                // Désérialiser les fichiers si présents
                List<string> fichiers = new List<string>();
                if (!string.IsNullOrEmpty(annonce.Fichiers))
                {
                    try
                    {
                        fichiers = JsonSerializer.Deserialize<List<string>>(annonce.Fichiers) ?? new List<string>();
                    }
                    catch (JsonException)
                    {
                        // Si la désérialisation échoue, on garde une liste vide
                        fichiers = new List<string>();
                    }
                }

                var annonceDto = new AnnonceDetailDto
                {
                    Id = annonce.Id,
                    Titre = annonce.Titre,
                    Contenu = annonce.Contenu,
                    Type = annonce.Type,
                    DatePublication = annonce.DatePublication,
                    ClasseCible = annonce.ClasseCible,
                    Fichiers = fichiers,
                    EnvoyerNotification = annonce.EnvoyerNotification,
                    EcoleNom = annonce.Ecole?.Nom ?? string.Empty,
                    CreatedById = annonce.CreatedById,
                    CreatedByNom = annonce.CreatedBy?.NomComplet ?? "Système",
                    CreatedAt = annonce.CreatedAt,
                    UpdatedAt = annonce.UpdatedAt
                };

                return Ok(annonceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'annonce {AnnonceId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'annonce");
            }
        }

        // POST: api/ecoles/{ecoleId}/annonces
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<AnnonceDetailDto>> CreateAnnonce(int ecoleId, [FromBody] CreateAnnonceRequest request)
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
                if (ecole == null || ecole.IsDeleted)
                {
                    return NotFound("École non trouvée");
                }

                // Vérifier que la classe cible existe si spécifiée
                if (!string.IsNullOrEmpty(request.ClasseCible))
                {
                    var classe = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Nom == request.ClasseCible &&
                                                c.EcoleId == ecoleId &&
                                                !c.IsDeleted);

                    if (classe == null)
                    {
                        return BadRequest($"Classe '{request.ClasseCible}' non trouvée dans cette école");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Sérialiser les fichiers si présents
                string? fichiersJson = null;
                if (request.Fichiers != null && request.Fichiers.Any())
                {
                    fichiersJson = JsonSerializer.Serialize(request.Fichiers);
                }

                var annonce = new Annonce
                {
                    Titre = request.Titre,
                    Contenu = request.Contenu,
                    Type = request.Type,
                    DatePublication = request.DatePublication ?? DateTime.UtcNow,
                    ClasseCible = request.ClasseCible,
                    Fichiers = fichiersJson,
                    EnvoyerNotification = request.EnvoyerNotification,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId ?? string.Empty
                };

                _context.Annonces.Add(annonce);
                await _context.SaveChangesAsync();

                // Recharger l'annonce avec les relations
                annonce = await _context.Annonces
                    .Include(a => a.CreatedBy)
                    .Include(a => a.Ecole)
                    .FirstAsync(a => a.Id == annonce.Id);

                _logger.LogInformation("Annonce '{Titre}' créée pour l'école {EcoleId} avec l'ID {Id}",
                    annonce.Titre, ecoleId, annonce.Id);

                var result = new AnnonceDetailDto
                {
                    Id = annonce.Id,
                    Titre = annonce.Titre,
                    Contenu = annonce.Contenu,
                    Type = annonce.Type,
                    DatePublication = annonce.DatePublication,
                    ClasseCible = annonce.ClasseCible,
                    Fichiers = request.Fichiers ?? new List<string>(),
                    EnvoyerNotification = annonce.EnvoyerNotification,
                    EcoleNom = annonce.Ecole?.Nom ?? string.Empty,
                    CreatedById = annonce.CreatedById,
                    CreatedByNom = annonce.CreatedBy?.NomComplet ?? "Système",
                    CreatedAt = annonce.CreatedAt
                };

                return CreatedAtAction(nameof(GetAnnonce), new { ecoleId, id = annonce.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'annonce pour l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'annonce");
            }
        }

        // PUT: api/ecoles/{ecoleId}/annonces/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateAnnonce(int ecoleId, int id, [FromBody] UpdateAnnonceRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'annonce est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageAnnonce(ecoleId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de modifier cette annonce");
                }

                var annonce = await _context.Annonces
                    .FirstOrDefaultAsync(a => a.Id == id && a.EcoleId == ecoleId && !a.IsDeleted);

                if (annonce == null)
                {
                    return NotFound($"Annonce avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                // Vérifier que la classe cible existe si spécifiée
                if (!string.IsNullOrEmpty(request.ClasseCible))
                {
                    var classe = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Nom == request.ClasseCible &&
                                                c.EcoleId == ecoleId &&
                                                !c.IsDeleted);

                    if (classe == null)
                    {
                        return BadRequest($"Classe '{request.ClasseCible}' non trouvée dans cette école");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Titre))
                    annonce.Titre = request.Titre;
                if (!string.IsNullOrEmpty(request.Contenu))
                    annonce.Contenu = request.Contenu;
                if (!string.IsNullOrEmpty(request.Type))
                    annonce.Type = request.Type;
                if (request.DatePublication.HasValue)
                    annonce.DatePublication = request.DatePublication.Value;
                if (request.ClasseCible != null)
                    annonce.ClasseCible = string.IsNullOrEmpty(request.ClasseCible) ? null : request.ClasseCible;
                if (request.EnvoyerNotification.HasValue)
                    annonce.EnvoyerNotification = request.EnvoyerNotification.Value;

                // Mettre à jour les fichiers si fournis
                if (request.Fichiers != null)
                {
                    annonce.Fichiers = request.Fichiers.Any() ? JsonSerializer.Serialize(request.Fichiers) : null;
                }

                annonce.UpdatedAt = DateTime.UtcNow;
                annonce.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Annonce {Id} mise à jour dans l'école {EcoleId}", id, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'annonce {AnnonceId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'annonce");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/annonces/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteAnnonce(int ecoleId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'annonce est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageAnnonce(ecoleId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de supprimer cette annonce");
                }

                var annonce = await _context.Annonces
                    .FirstOrDefaultAsync(a => a.Id == id && a.EcoleId == ecoleId && !a.IsDeleted);

                if (annonce == null)
                {
                    return NotFound($"Annonce avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                annonce.IsDeleted = true;
                annonce.UpdatedAt = DateTime.UtcNow;
                annonce.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Annonce '{Titre}' supprimée (soft delete) de l'école {EcoleId}",
                    annonce.Titre, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'annonce {AnnonceId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'annonce");
            }
        }

        // GET: api/ecoles/{ecoleId}/annonces/publiques
        [HttpGet("publiques")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<AnnonceListeDto>>> GetAnnoncesPubliques(
            int ecoleId,
            [FromQuery] string? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
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

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var query = _context.Annonces
                    .Where(a => a.EcoleId == ecoleId && !a.IsDeleted);

                // Si c'est un parent, filtrer par les classes de ses enfants
                if (User.IsInRole("Parent"))
                {
                    var classesEnfants = await _context.ParentEnfants
                        .Where(pe => pe.ParentId == currentUserId && !pe.IsDeleted)
                        .Include(pe => pe.Enfant)
                        .Where(pe => pe.Enfant.Classe != null)
                        .Select(pe => pe.Enfant.Classe!.Nom)
                        .Distinct()
                        .ToListAsync();

                    // Inclure les annonces générales (sans classe cible) et celles ciblant les classes des enfants
                    query = query.Where(a => a.ClasseCible == null || classesEnfants.Contains(a.ClasseCible));
                }

                // Filtrage par type
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(a => a.Type == type);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var annonces = await query
                    .Include(a => a.CreatedBy)
                    .OrderByDescending(a => a.DatePublication)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new AnnonceListeDto
                    {
                        Id = a.Id,
                        Titre = a.Titre,
                        Contenu = a.Contenu.Length > 150 ? a.Contenu.Substring(0, 150) + "..." : a.Contenu,
                        Type = a.Type,
                        DatePublication = a.DatePublication,
                        ClasseCible = a.ClasseCible,
                        EnvoyerNotification = a.EnvoyerNotification,
                        CreatedById = a.CreatedById,
                        CreatedByNom = a.CreatedBy != null ? a.CreatedBy.NomComplet : "Administration",
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(annonces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des annonces publiques de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des annonces publiques");
            }
        }

        // GET: api/ecoles/{ecoleId}/annonces/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<AnnonceStatistiquesDto>> GetStatistiques(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var annonces = await _context.Annonces
                    .Where(a => a.EcoleId == ecoleId && !a.IsDeleted)
                    .ToListAsync();

                var ecole = await _context.Ecoles.FindAsync(ecoleId);

                var statistiques = new AnnonceStatistiquesDto
                {
                    EcoleId = ecoleId,
                    EcoleNom = ecole?.Nom ?? string.Empty,
                    NombreTotalAnnonces = annonces.Count,
                    NombreAnnoncesGenerales = annonces.Count(a => string.IsNullOrEmpty(a.ClasseCible)),
                    NombreAnnoncesCiblees = annonces.Count(a => !string.IsNullOrEmpty(a.ClasseCible)),
                    NombreAvecNotification = annonces.Count(a => a.EnvoyerNotification),
                    AnnoncesParType = annonces.GroupBy(a => a.Type)
                        .Select(g => new AnnonceTypeStatDto
                        {
                            Type = g.Key,
                            Nombre = g.Count(),
                            NombreAvecNotification = g.Count(a => a.EnvoyerNotification),
                            NombreCiblees = g.Count(a => !string.IsNullOrEmpty(a.ClasseCible))
                        })
                        .OrderByDescending(t => t.Nombre)
                        .ToList(),
                    AnnoncesParMois = annonces
                        .Where(a => a.DatePublication >= DateTime.Now.AddMonths(-6))
                        .GroupBy(a => new { a.DatePublication.Year, a.DatePublication.Month })
                        .Select(g => new AnnonceMoisStatDto
                        {
                            Annee = g.Key.Year,
                            Mois = g.Key.Month,
                            NombreAnnonces = g.Count()
                        })
                        .OrderByDescending(m => m.Annee)
                        .ThenByDescending(m => m.Mois)
                        .ToList()
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des annonces de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/ecoles/{ecoleId}/annonces/types
        [HttpGet("types")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public IActionResult GetTypesAnnonce()
        {
            var types = new[]
            {
                new { Value = "generale", Label = "Générale" },
                new { Value = "cantine", Label = "Cantine" },
                new { Value = "activite", Label = "Activité" },
                new { Value = "urgent", Label = "Urgent" },
                new { Value = "information", Label = "Information" },
                new { Value = "pedagogique", Label = "Pédagogique" },
                new { Value = "administratif", Label = "Administratif" }
            };

            return Ok(types);
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

        private async Task<bool> CanManageAnnonce(int ecoleId, int annonceId)
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

            // Seuls SuperAdmin et Admin peuvent gérer les annonces
            return false;
        }
    }

    // DTOs pour les annonces
    public class AnnonceListeDto
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Contenu { get; set; } = string.Empty; // Tronqué
        public string Type { get; set; } = string.Empty;
        public DateTime DatePublication { get; set; }
        public string? ClasseCible { get; set; }
        public bool EnvoyerNotification { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        public string CreatedByNom { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AnnonceDetailDto : AnnonceListeDto
    {
        public new string Contenu { get; set; } = string.Empty; // Contenu complet
        public List<string> Fichiers { get; set; } = new List<string>();
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAnnonceRequest
    {
        [Required(ErrorMessage = "Le titre est obligatoire")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu est obligatoire")]
        public string Contenu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le type est obligatoire")]
        public string Type { get; set; } = "generale";

        public DateTime? DatePublication { get; set; }
        public string? ClasseCible { get; set; }
        public List<string>? Fichiers { get; set; }
        public bool EnvoyerNotification { get; set; } = false;
    }

    public class UpdateAnnonceRequest
    {
        public string? Titre { get; set; }
        public string? Contenu { get; set; }
        public string? Type { get; set; }
        public DateTime? DatePublication { get; set; }
        public string? ClasseCible { get; set; }
        public List<string>? Fichiers { get; set; }
        public bool? EnvoyerNotification { get; set; }
    }

    public class AnnonceStatistiquesDto
    {
        public int EcoleId { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public int NombreTotalAnnonces { get; set; }
        public int NombreAnnoncesGenerales { get; set; }
        public int NombreAnnoncesCiblees { get; set; }
        public int NombreAvecNotification { get; set; }
        public List<AnnonceTypeStatDto> AnnoncesParType { get; set; } = new List<AnnonceTypeStatDto>();
        public List<AnnonceMoisStatDto> AnnoncesParMois { get; set; } = new List<AnnonceMoisStatDto>();
    }

    public class AnnonceTypeStatDto
    {
        public string Type { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public int NombreAvecNotification { get; set; }
        public int NombreCiblees { get; set; }
    }

    public class AnnonceMoisStatDto
    {
        public int Annee { get; set; }
        public int Mois { get; set; }
        public int NombreAnnonces { get; set; }
    }
}