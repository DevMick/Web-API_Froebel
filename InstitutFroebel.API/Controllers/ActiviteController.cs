using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/activites")]
    [ApiController]
    [Authorize]
    public class ActiviteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActiviteController> _logger;

        public ActiviteController(
            ApplicationDbContext context,
            ILogger<ActiviteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/activites
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<ActiviteListeDto>>> GetActivites(
            int ecoleId,
            [FromQuery] string? classeConcernee = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] bool? prochaines = null,
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

                var query = _context.Activites
                    .Where(a => a.EcoleId == ecoleId && !a.IsDeleted);

                // Filtrage par classe concernée
                if (!string.IsNullOrEmpty(classeConcernee))
                {
                    query = query.Where(a => a.ClasseConcernee == classeConcernee || a.ClasseConcernee == null);
                }

                // Filtrage par période
                if (dateDebut.HasValue)
                {
                    query = query.Where(a => a.DateDebut >= dateDebut.Value);
                }
                if (dateFin.HasValue)
                {
                    query = query.Where(a => a.DateDebut <= dateFin.Value);
                }

                // Filtrage prochaines activités
                if (prochaines.HasValue && prochaines.Value)
                {
                    query = query.Where(a => a.DateDebut >= DateTime.Today);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(a => a.Nom.ToLower().Contains(termeLower) ||
                                           (a.Description != null && a.Description.ToLower().Contains(termeLower)) ||
                                           (a.Lieu != null && a.Lieu.ToLower().Contains(termeLower)));
                }

                // Si c'est un parent, filtrer par les classes de ses enfants
                if (User.IsInRole("Parent"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var classesEnfants = await _context.ParentEnfants
                        .Where(pe => pe.ParentId == currentUserId && !pe.IsDeleted)
                        .Include(pe => pe.Enfant)
                        .Where(pe => pe.Enfant.Classe != null)
                        .Select(pe => pe.Enfant.Classe!.Nom)
                        .Distinct()
                        .ToListAsync();

                    // Inclure les activités générales (sans classe) et celles concernant les classes des enfants
                    query = query.Where(a => a.ClasseConcernee == null || classesEnfants.Contains(a.ClasseConcernee));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var activites = await query
                    .Include(a => a.CreatedBy)
                    .OrderBy(a => a.DateDebut)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new ActiviteListeDto
                    {
                        Id = a.Id,
                        Nom = a.Nom,
                        Description = a.Description != null && a.Description.Length > 150
                            ? a.Description.Substring(0, 150) + "..."
                            : a.Description,
                        DateDebut = a.DateDebut,
                        DateFin = a.DateFin,
                        HeureDebut = a.HeureDebut,
                        HeureFin = a.HeureFin,
                        Lieu = a.Lieu,
                        ClasseConcernee = a.ClasseConcernee,
                        CreatedByNom = a.CreatedBy != null ? a.CreatedBy.NomComplet : "Administration",
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(activites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des activités de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des activités");
            }
        }

        // GET: api/ecoles/{ecoleId}/activites/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<ActiviteDetailDto>> GetActivite(int ecoleId, int id)
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
                    return BadRequest("L'identifiant de l'activité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var activite = await _context.Activites
                    .Include(a => a.CreatedBy)
                    .Include(a => a.Ecole)
                    .Where(a => a.Id == id && a.EcoleId == ecoleId && !a.IsDeleted)
                    .FirstOrDefaultAsync();

                if (activite == null)
                {
                    return NotFound($"Activité avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                // Vérifier si c'est un parent et s'il peut accéder à cette activité
                if (User.IsInRole("Parent"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(activite.ClasseConcernee))
                    {
                        var classesEnfants = await _context.ParentEnfants
                            .Where(pe => pe.ParentId == currentUserId && !pe.IsDeleted)
                            .Include(pe => pe.Enfant)
                            .Where(pe => pe.Enfant.Classe != null)
                            .Select(pe => pe.Enfant.Classe!.Nom)
                            .ToListAsync();

                        if (!classesEnfants.Contains(activite.ClasseConcernee))
                        {
                            return Forbid("Cette activité ne concerne pas vos enfants");
                        }
                    }
                }

                var activiteDto = new ActiviteDetailDto
                {
                    Id = activite.Id,
                    Nom = activite.Nom,
                    Description = activite.Description,
                    DateDebut = activite.DateDebut,
                    DateFin = activite.DateFin,
                    HeureDebut = activite.HeureDebut,
                    HeureFin = activite.HeureFin,
                    Lieu = activite.Lieu,
                    ClasseConcernee = activite.ClasseConcernee,
                    EcoleNom = activite.Ecole?.Nom ?? string.Empty,
                    CreatedByNom = activite.CreatedBy?.NomComplet ?? "Administration",
                    CreatedAt = activite.CreatedAt,
                    UpdatedAt = activite.UpdatedAt
                };

                return Ok(activiteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'activité {ActiviteId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'activité");
            }
        }

        // POST: api/ecoles/{ecoleId}/activites
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ActiviteDetailDto>> CreateActivite(int ecoleId, [FromBody] CreateActiviteRequest request)
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

                // Vérifier que la classe concernée existe si spécifiée
                if (!string.IsNullOrEmpty(request.ClasseConcernee))
                {
                    var classe = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Nom == request.ClasseConcernee &&
                                                c.EcoleId == ecoleId &&
                                                !c.IsDeleted);

                    if (classe == null)
                    {
                        return BadRequest($"Classe '{request.ClasseConcernee}' non trouvée dans cette école");
                    }
                }

                // Validation des dates
                if (request.DateFin.HasValue && request.DateFin.Value < request.DateDebut)
                {
                    return BadRequest("La date de fin ne peut pas être antérieure à la date de début");
                }

                // Validation des heures
                if (request.HeureDebut.HasValue && request.HeureFin.HasValue &&
                    request.HeureFin.Value <= request.HeureDebut.Value)
                {
                    return BadRequest("L'heure de fin doit être postérieure à l'heure de début");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var activite = new Activite
                {
                    Nom = request.Nom,
                    Description = request.Description,
                    DateDebut = request.DateDebut,
                    DateFin = request.DateFin,
                    HeureDebut = request.HeureDebut,
                    HeureFin = request.HeureFin,
                    Lieu = request.Lieu,
                    ClasseConcernee = request.ClasseConcernee,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId
                };

                _context.Activites.Add(activite);
                await _context.SaveChangesAsync();

                // Recharger l'activité avec les relations
                activite = await _context.Activites
                    .Include(a => a.CreatedBy)
                    .Include(a => a.Ecole)
                    .FirstAsync(a => a.Id == activite.Id);

                _logger.LogInformation("Activité '{Nom}' créée pour l'école {EcoleId} avec l'ID {Id}",
                    activite.Nom, ecoleId, activite.Id);

                var result = new ActiviteDetailDto
                {
                    Id = activite.Id,
                    Nom = activite.Nom,
                    Description = activite.Description,
                    DateDebut = activite.DateDebut,
                    DateFin = activite.DateFin,
                    HeureDebut = activite.HeureDebut,
                    HeureFin = activite.HeureFin,
                    Lieu = activite.Lieu,
                    ClasseConcernee = activite.ClasseConcernee,
                    EcoleNom = activite.Ecole?.Nom ?? string.Empty,
                    CreatedByNom = activite.CreatedBy?.NomComplet ?? "Administration",
                    CreatedAt = activite.CreatedAt
                };

                return CreatedAtAction(nameof(GetActivite), new { ecoleId, id = activite.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'activité pour l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'activité");
            }
        }

        // PUT: api/ecoles/{ecoleId}/activites/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateActivite(int ecoleId, int id, [FromBody] UpdateActiviteRequest request)
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
                    return BadRequest("L'identifiant de l'activité est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageActivite(ecoleId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de modifier cette activité");
                }

                var activite = await _context.Activites
                    .FirstOrDefaultAsync(a => a.Id == id && a.EcoleId == ecoleId && !a.IsDeleted);

                if (activite == null)
                {
                    return NotFound($"Activité avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                // Vérifier que la classe concernée existe si spécifiée
                if (!string.IsNullOrEmpty(request.ClasseConcernee))
                {
                    var classe = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Nom == request.ClasseConcernee &&
                                                c.EcoleId == ecoleId &&
                                                !c.IsDeleted);

                    if (classe == null)
                    {
                        return BadRequest($"Classe '{request.ClasseConcernee}' non trouvée dans cette école");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom))
                    activite.Nom = request.Nom;
                if (request.Description != null)
                    activite.Description = string.IsNullOrEmpty(request.Description) ? null : request.Description;
                if (request.DateDebut.HasValue)
                    activite.DateDebut = request.DateDebut.Value;
                if (request.DateFin != null)
                    activite.DateFin = request.DateFin;
                if (request.HeureDebut != null)
                    activite.HeureDebut = request.HeureDebut;
                if (request.HeureFin != null)
                    activite.HeureFin = request.HeureFin;
                if (request.Lieu != null)
                    activite.Lieu = string.IsNullOrEmpty(request.Lieu) ? null : request.Lieu;
                if (request.ClasseConcernee != null)
                    activite.ClasseConcernee = string.IsNullOrEmpty(request.ClasseConcernee) ? null : request.ClasseConcernee;

                // Validation des dates après mise à jour
                if (activite.DateFin.HasValue && activite.DateFin.Value < activite.DateDebut)
                {
                    return BadRequest("La date de fin ne peut pas être antérieure à la date de début");
                }

                // Validation des heures après mise à jour
                if (activite.HeureDebut.HasValue && activite.HeureFin.HasValue &&
                    activite.HeureFin.Value <= activite.HeureDebut.Value)
                {
                    return BadRequest("L'heure de fin doit être postérieure à l'heure de début");
                }

                activite.UpdatedAt = DateTime.UtcNow;
                activite.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Activité {Id} mise à jour dans l'école {EcoleId}", id, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'activité {ActiviteId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'activité");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/activites/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteActivite(int ecoleId, int id)
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
                    return BadRequest("L'identifiant de l'activité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageActivite(ecoleId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de supprimer cette activité");
                }

                var activite = await _context.Activites
                    .FirstOrDefaultAsync(a => a.Id == id && a.EcoleId == ecoleId && !a.IsDeleted);

                if (activite == null)
                {
                    return NotFound($"Activité avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                activite.IsDeleted = true;
                activite.UpdatedAt = DateTime.UtcNow;
                activite.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Activité '{Nom}' supprimée (soft delete) de l'école {EcoleId}",
                    activite.Nom, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'activité {ActiviteId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'activité");
            }
        }

        // GET: api/ecoles/{ecoleId}/activites/calendrier
        [HttpGet("calendrier")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<ActiviteCalendrierDto>>> GetCalendrierActivites(
            int ecoleId,
            [FromQuery] DateTime? mois = null,
            [FromQuery] string? classeConcernee = null)
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

                // Définir les dates de début et fin du mois
                var dateRef = mois ?? DateTime.Today;
                var debutMois = new DateTime(dateRef.Year, dateRef.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);

                var query = _context.Activites
                    .Where(a => a.EcoleId == ecoleId && !a.IsDeleted)
                    .Where(a => a.DateDebut >= debutMois && a.DateDebut <= finMois);

                // Filtrage par classe
                if (!string.IsNullOrEmpty(classeConcernee))
                {
                    query = query.Where(a => a.ClasseConcernee == classeConcernee || a.ClasseConcernee == null);
                }

                // Si c'est un parent, filtrer par les classes de ses enfants
                if (User.IsInRole("Parent"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var classesEnfants = await _context.ParentEnfants
                        .Where(pe => pe.ParentId == currentUserId && !pe.IsDeleted)
                        .Include(pe => pe.Enfant)
                        .Where(pe => pe.Enfant.Classe != null)
                        .Select(pe => pe.Enfant.Classe!.Nom)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(a => a.ClasseConcernee == null || classesEnfants.Contains(a.ClasseConcernee));
                }

                var activites = await query
                    .OrderBy(a => a.DateDebut)
                    .ThenBy(a => a.HeureDebut)
                    .Select(a => new ActiviteCalendrierDto
                    {
                        Id = a.Id,
                        Nom = a.Nom,
                        DateDebut = a.DateDebut,
                        DateFin = a.DateFin,
                        HeureDebut = a.HeureDebut,
                        HeureFin = a.HeureFin,
                        Lieu = a.Lieu,
                        ClasseConcernee = a.ClasseConcernee
                    })
                    .ToListAsync();

                return Ok(activites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du calendrier des activités de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du calendrier");
            }
        }

        // GET: api/ecoles/{ecoleId}/activites/prochaines
        [HttpGet("prochaines")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<ActiviteListeDto>>> GetProchainesActivites(
            int ecoleId,
            [FromQuery] int limite = 5)
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

                var query = _context.Activites
                    .Where(a => a.EcoleId == ecoleId && !a.IsDeleted)
                    .Where(a => a.DateDebut >= DateTime.Today);

                // Si c'est un parent, filtrer par les classes de ses enfants
                if (User.IsInRole("Parent"))
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var classesEnfants = await _context.ParentEnfants
                        .Where(pe => pe.ParentId == currentUserId && !pe.IsDeleted)
                        .Include(pe => pe.Enfant)
                        .Where(pe => pe.Enfant.Classe != null)
                        .Select(pe => pe.Enfant.Classe!.Nom)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(a => a.ClasseConcernee == null || classesEnfants.Contains(a.ClasseConcernee));
                }

                var activites = await query
                    .Include(a => a.CreatedBy)
                    .OrderBy(a => a.DateDebut)
                    .ThenBy(a => a.HeureDebut)
                    .Take(limite)
                    .Select(a => new ActiviteListeDto
                    {
                        Id = a.Id,
                        Nom = a.Nom,
                        Description = a.Description != null && a.Description.Length > 100
                            ? a.Description.Substring(0, 100) + "..."
                            : a.Description,
                        DateDebut = a.DateDebut,
                        DateFin = a.DateFin,
                        HeureDebut = a.HeureDebut,
                        HeureFin = a.HeureFin,
                        Lieu = a.Lieu,
                        ClasseConcernee = a.ClasseConcernee,
                        CreatedByNom = a.CreatedBy != null ? a.CreatedBy.NomComplet : "Administration",
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(activites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prochaines activités de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des prochaines activités");
            }
        }

        // GET: api/ecoles/{ecoleId}/activites/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ActiviteStatistiquesDto>> GetStatistiques(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var activites = await _context.Activites
                    .Where(a => a.EcoleId == ecoleId && !a.IsDeleted)
                    .ToListAsync();

                var ecole = await _context.Ecoles.FindAsync(ecoleId);

                var maintenant = DateTime.Now;
                var debutMois = new DateTime(maintenant.Year, maintenant.Month, 1);
                var debutAnnee = new DateTime(maintenant.Year, 1, 1);

                var statistiques = new ActiviteStatistiquesDto
                {
                    EcoleId = ecoleId,
                    EcoleNom = ecole?.Nom ?? string.Empty,
                    NombreTotalActivites = activites.Count,
                    NombreActivitesGenerales = activites.Count(a => string.IsNullOrEmpty(a.ClasseConcernee)),
                    NombreActivitesCiblees = activites.Count(a => !string.IsNullOrEmpty(a.ClasseConcernee)),
                    NombreActivitesProchaintes = activites.Count(a => a.DateDebut >= DateTime.Today),
                    NombreActivitesCeMois = activites.Count(a => a.DateDebut >= debutMois && a.DateDebut < debutMois.AddMonths(1)),
                    NombreActivitesCetteAnnee = activites.Count(a => a.DateDebut >= debutAnnee),
                    ActivitesParMois = activites
                        .Where(a => a.DateDebut >= DateTime.Now.AddMonths(-6))
                        .GroupBy(a => new { a.DateDebut.Year, a.DateDebut.Month })
                        .Select(g => new ActiviteMoisStatDto
                        {
                            Annee = g.Key.Year,
                            Mois = g.Key.Month,
                            NombreActivites = g.Count()
                        })
                        .OrderByDescending(m => m.Annee)
                        .ThenByDescending(m => m.Mois)
                        .ToList()
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des activités de l'école {EcoleId}", ecoleId);
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

        private async Task<bool> CanManageActivite(int ecoleId, int activiteId)
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

            // Seuls SuperAdmin et Admin peuvent gérer les activités
            return false;
        }
    }

    // DTOs pour les activités
    public class ActiviteListeDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public TimeSpan? HeureDebut { get; set; }
        public TimeSpan? HeureFin { get; set; }
        public string? Lieu { get; set; }
        public string? ClasseConcernee { get; set; }
        public string CreatedByNom { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ActiviteDetailDto : ActiviteListeDto
    {
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class ActiviteCalendrierDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public TimeSpan? HeureDebut { get; set; }
        public TimeSpan? HeureFin { get; set; }
        public string? Lieu { get; set; }
        public string? ClasseConcernee { get; set; }
    }

    public class CreateActiviteRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "La date de début est obligatoire")]
        public DateTime DateDebut { get; set; }

        public DateTime? DateFin { get; set; }
        public TimeSpan? HeureDebut { get; set; }
        public TimeSpan? HeureFin { get; set; }
        public string? Lieu { get; set; }
        public string? ClasseConcernee { get; set; }
    }

    public class UpdateActiviteRequest
    {
        public string? Nom { get; set; }
        public string? Description { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public TimeSpan? HeureDebut { get; set; }
        public TimeSpan? HeureFin { get; set; }
        public string? Lieu { get; set; }
        public string? ClasseConcernee { get; set; }
    }

    public class ActiviteStatistiquesDto
    {
        public int EcoleId { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public int NombreTotalActivites { get; set; }
        public int NombreActivitesGenerales { get; set; }
        public int NombreActivitesCiblees { get; set; }
        public int NombreActivitesProchaintes { get; set; }
        public int NombreActivitesCeMois { get; set; }
        public int NombreActivitesCetteAnnee { get; set; }
        public List<ActiviteMoisStatDto> ActivitesParMois { get; set; } = new List<ActiviteMoisStatDto>();
    }

    public class ActiviteMoisStatDto
    {
        public int Annee { get; set; }
        public int Mois { get; set; }
        public int NombreActivites { get; set; }
    }
}