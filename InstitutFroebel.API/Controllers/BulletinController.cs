using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins")]
    [ApiController]
    [Authorize]
    public class BulletinController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BulletinController> _logger;

        public BulletinController(
            ApplicationDbContext context,
            ILogger<BulletinController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<BulletinListeDto>>> GetBulletins(
            int ecoleId,
            int enfantId,
            [FromQuery] string? trimestre = null,
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

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEnfant(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé à cet enfant");
                }

                // Vérifier que l'enfant existe
                var enfant = await _context.Enfants
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.EcoleId == ecoleId && !e.IsDeleted);

                if (enfant == null)
                {
                    return NotFound($"Enfant avec l'ID {enfantId} non trouvé dans l'école {ecoleId}");
                }

                var query = _context.Bulletins
                    .Where(b => b.EnfantId == enfantId && !b.IsDeleted);

                // Filtrage par trimestre
                if (!string.IsNullOrEmpty(trimestre))
                {
                    query = query.Where(b => b.Trimestre == trimestre);
                }

                // Filtrage par année scolaire
                if (!string.IsNullOrEmpty(anneeScolaire))
                {
                    query = query.Where(b => b.AnneeScolaire == anneeScolaire);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var bulletins = await query
                    .Include(b => b.Enfant)
                        .ThenInclude(e => e.ParentsEnfants)
                            .ThenInclude(pe => pe.Parent)
                    .Include(b => b.Enfant.Classe)
                    .OrderByDescending(b => b.AnneeScolaire)
                    .ThenByDescending(b => b.Trimestre)
                    .ThenByDescending(b => b.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new BulletinListeDto
                    {
                        Id = b.Id,
                        Trimestre = b.Trimestre,
                        AnneeScolaire = b.AnneeScolaire,
                        NomFichier = b.NomFichier,
                        TailleFichier = b.FichierBulletin.Length,
                        EnfantId = b.EnfantId,
                        EnfantNom = $"{b.Enfant.Prenom} {b.Enfant.Nom}",
                        ClasseNom = b.Enfant.Classe != null ? b.Enfant.Classe.Nom : null,
                        ParentNom = b.Enfant.ParentsEnfants.Where(pe => !pe.IsDeleted).Select(pe => pe.Parent.NomComplet).FirstOrDefault() ?? string.Empty,
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(bulletins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des bulletins de l'enfant {EnfantId} de l'école {EcoleId}", enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des bulletins");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<BulletinDetailDto>> GetBulletin(int ecoleId, int enfantId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant du bulletin est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEnfant(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé à cet enfant");
                }

                var bulletin = await _context.Bulletins
                    .Include(b => b.Enfant)
                        .ThenInclude(e => e.ParentsEnfants)
                            .ThenInclude(pe => pe.Parent)
                    .Include(b => b.Enfant.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(b => b.Enfant.Ecole)
                    .Where(b => b.Id == id && b.EnfantId == enfantId && !b.IsDeleted)
                    .FirstOrDefaultAsync();

                if (bulletin == null)
                {
                    return NotFound($"Bulletin avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                var bulletinDto = new BulletinDetailDto
                {
                    Id = bulletin.Id,
                    Trimestre = bulletin.Trimestre,
                    AnneeScolaire = bulletin.AnneeScolaire,
                    NomFichier = bulletin.NomFichier,
                    TailleFichier = bulletin.FichierBulletin.Length,
                    EnfantId = bulletin.EnfantId,
                    EnfantNom = $"{bulletin.Enfant.Prenom} {bulletin.Enfant.Nom}",
                    EnfantDateNaissance = bulletin.Enfant.DateNaissance,
                    ClasseNom = bulletin.Enfant.Classe?.Nom,
                    EnseignantPrincipalNom = bulletin.Enfant.Classe?.EnseignantPrincipal?.NomComplet,
                    ParentNom = bulletin.Enfant.ParentsEnfants.Where(pe => !pe.IsDeleted).Select(pe => pe.Parent.NomComplet).FirstOrDefault() ?? string.Empty,
                    ParentEmail = bulletin.Enfant.ParentsEnfants.Where(pe => !pe.IsDeleted).Select(pe => pe.Parent.Email).FirstOrDefault(),
                    EcoleNom = bulletin.Enfant.Ecole?.Nom ?? string.Empty,
                    CreatedAt = bulletin.CreatedAt,
                    UpdatedAt = bulletin.UpdatedAt
                };

                return Ok(bulletinDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du bulletin {BulletinId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du bulletin");
            }
        }

        // POST: api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<BulletinDetailDto>> CreateBulletin(int ecoleId, int enfantId, [FromForm] CreateBulletinRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageEnfant(ecoleId, enfantId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cet enfant");
                }

                // Vérifier que l'enfant existe
                var enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                    .Include(e => e.Classe)
                    .Include(e => e.Ecole)
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.EcoleId == ecoleId && !e.IsDeleted);

                if (enfant == null)
                {
                    return NotFound($"Enfant avec l'ID {enfantId} non trouvé dans l'école {ecoleId}");
                }

                // Vérifier l'unicité du bulletin (enfant + trimestre + année scolaire)
                var existingBulletin = await _context.Bulletins
                    .AnyAsync(b => b.EnfantId == enfantId &&
                                 b.Trimestre == request.Trimestre &&
                                 b.AnneeScolaire == request.AnneeScolaire &&
                                 !b.IsDeleted);

                if (existingBulletin)
                {
                    return BadRequest($"Un bulletin existe déjà pour le trimestre {request.Trimestre} de l'année scolaire {request.AnneeScolaire}");
                }

                // Valider et lire le fichier
                if (request.Fichier == null || request.Fichier.Length == 0)
                {
                    return BadRequest("Le fichier est obligatoire");
                }

                // Limite de taille (par exemple 15MB)
                const long maxFileSize = 15 * 1024 * 1024;
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

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var bulletin = new Bulletin
                {
                    EnfantId = enfantId,
                    Trimestre = request.Trimestre,
                    AnneeScolaire = request.AnneeScolaire,
                    NomFichier = request.NomFichier,
                    FichierBulletin = fichierBytes,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId
                };

                _context.Bulletins.Add(bulletin);
                await _context.SaveChangesAsync();

                // Recharger le bulletin avec les relations
                bulletin = await _context.Bulletins
                    .Include(b => b.Enfant)
                        .ThenInclude(e => e.ParentsEnfants)
                            .ThenInclude(pe => pe.Parent)
                    .Include(b => b.Enfant.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(b => b.Enfant.Ecole)
                    .FirstAsync(b => b.Id == bulletin.Id);

                _logger.LogInformation("Bulletin '{NomFichier}' créé pour l'enfant {EnfantId} (T{Trimestre} {AnneeScolaire}) avec l'ID {Id}",
                    bulletin.NomFichier, enfantId, request.Trimestre, request.AnneeScolaire, bulletin.Id);

                var result = new BulletinDetailDto
                {
                    Id = bulletin.Id,
                    Trimestre = bulletin.Trimestre,
                    AnneeScolaire = bulletin.AnneeScolaire,
                    NomFichier = bulletin.NomFichier,
                    TailleFichier = bulletin.FichierBulletin.Length,
                    EnfantId = bulletin.EnfantId,
                    EnfantNom = $"{bulletin.Enfant.Prenom} {bulletin.Enfant.Nom}",
                    EnfantDateNaissance = bulletin.Enfant.DateNaissance,
                    ClasseNom = bulletin.Enfant.Classe?.Nom,
                    EnseignantPrincipalNom = bulletin.Enfant.Classe?.EnseignantPrincipal?.NomComplet,
                    ParentNom = bulletin.Enfant.ParentsEnfants.Where(pe => !pe.IsDeleted).Select(pe => pe.Parent.NomComplet).FirstOrDefault() ?? string.Empty,
                    ParentEmail = bulletin.Enfant.ParentsEnfants.Where(pe => !pe.IsDeleted).Select(pe => pe.Parent.Email).FirstOrDefault(),
                    EcoleNom = bulletin.Enfant.Ecole?.Nom ?? string.Empty,
                    CreatedAt = bulletin.CreatedAt
                };

                return CreatedAtAction(nameof(GetBulletin), new { ecoleId, enfantId, id = bulletin.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du bulletin pour l'enfant {EnfantId} de l'école {EcoleId}", enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création du bulletin");
            }
        }

        // PUT: api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<IActionResult> UpdateBulletin(int ecoleId, int enfantId, int id, [FromForm] UpdateBulletinRequest request)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant du bulletin est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageEnfant(ecoleId, enfantId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cet enfant");
                }

                var bulletin = await _context.Bulletins
                    .FirstOrDefaultAsync(b => b.Id == id && b.EnfantId == enfantId && !b.IsDeleted);

                if (bulletin == null)
                {
                    return NotFound($"Bulletin avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                // Vérifier l'unicité si trimestre ou année scolaire modifiés
                if ((!string.IsNullOrEmpty(request.Trimestre) && request.Trimestre != bulletin.Trimestre) ||
                    (!string.IsNullOrEmpty(request.AnneeScolaire) && request.AnneeScolaire != bulletin.AnneeScolaire))
                {
                    var newTrimestre = request.Trimestre ?? bulletin.Trimestre;
                    var newAnneeScolaire = request.AnneeScolaire ?? bulletin.AnneeScolaire;

                    var existingBulletin = await _context.Bulletins
                        .AnyAsync(b => b.Id != id &&
                                     b.EnfantId == enfantId &&
                                     b.Trimestre == newTrimestre &&
                                     b.AnneeScolaire == newAnneeScolaire &&
                                     !b.IsDeleted);

                    if (existingBulletin)
                    {
                        return BadRequest($"Un bulletin existe déjà pour le trimestre {newTrimestre} de l'année scolaire {newAnneeScolaire}");
                    }
                }

                // Traiter le nouveau fichier si fourni
                if (request.Fichier != null && request.Fichier.Length > 0)
                {
                    // Limite de taille (par exemple 15MB)
                    const long maxFileSize = 15 * 1024 * 1024;
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

                    bulletin.FichierBulletin = fichierBytes;
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Trimestre))
                    bulletin.Trimestre = request.Trimestre;
                if (!string.IsNullOrEmpty(request.AnneeScolaire))
                    bulletin.AnneeScolaire = request.AnneeScolaire;
                if (!string.IsNullOrEmpty(request.NomFichier))
                    bulletin.NomFichier = request.NomFichier;

                bulletin.UpdatedAt = DateTime.UtcNow;
                bulletin.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulletin {Id} mis à jour pour l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du bulletin {BulletinId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du bulletin");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<IActionResult> DeleteBulletin(int ecoleId, int enfantId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant du bulletin est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageEnfant(ecoleId, enfantId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cet enfant");
                }

                var bulletin = await _context.Bulletins
                    .FirstOrDefaultAsync(b => b.Id == id && b.EnfantId == enfantId && !b.IsDeleted);

                if (bulletin == null)
                {
                    return NotFound($"Bulletin avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                bulletin.IsDeleted = true;
                bulletin.UpdatedAt = DateTime.UtcNow;
                bulletin.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Bulletin '{NomFichier}' (T{Trimestre} {AnneeScolaire}) supprimé (soft delete) pour l'enfant {EnfantId}",
                    bulletin.NomFichier, bulletin.Trimestre, bulletin.AnneeScolaire, enfantId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du bulletin {BulletinId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du bulletin");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins/{id}/telecharger
        [HttpGet("{id:int}/telecharger")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<IActionResult> TelechargerBulletin(int ecoleId, int enfantId, int id)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (enfantId <= 0)
                {
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                if (id <= 0)
                {
                    return BadRequest("L'identifiant du bulletin est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEnfant(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé à cet enfant");
                }

                var bulletin = await _context.Bulletins
                    .FirstOrDefaultAsync(b => b.Id == id && b.EnfantId == enfantId && !b.IsDeleted);

                if (bulletin == null)
                {
                    return NotFound($"Bulletin avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                // Déterminer le type MIME basé sur l'extension du fichier
                var contentType = GetContentType(bulletin.NomFichier);

                return File(bulletin.FichierBulletin, contentType, bulletin.NomFichier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du bulletin {BulletinId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors du téléchargement du bulletin");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/{enfantId}/bulletins/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<BulletinStatistiquesDto>> GetStatistiques(int ecoleId, int enfantId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEnfant(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé à cet enfant");
                }

                var bulletins = await _context.Bulletins
                    .Where(b => b.EnfantId == enfantId && !b.IsDeleted)
                    .ToListAsync();

                var enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                    .FirstOrDefaultAsync(e => e.Id == enfantId);

                var statistiques = new BulletinStatistiquesDto
                {
                    EnfantId = enfantId,
                    EnfantNom = enfant != null ? $"{enfant.Prenom} {enfant.Nom}" : string.Empty,
                    ClasseNom = enfant?.Classe?.Nom,
                    ParentNom = enfant?.ParentsEnfants.Where(pe => !pe.IsDeleted).Select(pe => pe.Parent.NomComplet).FirstOrDefault() ?? string.Empty,
                    NombreTotalBulletins = bulletins.Count,
                    TailleTotaleBulletins = bulletins.Sum(b => (long)b.FichierBulletin.Length),
                    AnnesScolaires = bulletins.GroupBy(b => b.AnneeScolaire)
                        .Select(g => new AnneeScolaireBulletinStatDto
                        {
                            AnneeScolaire = g.Key,
                            NombreBulletins = g.Count(),
                            NombreTrimestres = g.Select(b => b.Trimestre).Distinct().Count(),
                            TailleTotal = g.Sum(b => (long)b.FichierBulletin.Length)
                        })
                        .OrderByDescending(a => a.AnneeScolaire)
                        .ToList()
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des bulletins de l'enfant {EnfantId} de l'école {EcoleId}", enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // Méthodes d'aide
        private async Task<bool> CanAccessEnfant(int ecoleId, int enfantId)
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
                var enfant = await _context.Enfants
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.ParentsEnfants.Any(pe => !pe.IsDeleted && pe.ParentId == userId));
                return enfant != null;
            }

            return false;
        }

        private async Task<bool> CanManageEnfant(int ecoleId, int enfantId)
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
                // Vérifier si l'enseignant est le principal de la classe de cet enfant
                var enfant = await _context.Enfants
                    .Include(e => e.Classe)
                    .FirstOrDefaultAsync(e => e.Id == enfantId);

                return enfant?.Classe?.EnseignantPrincipalId == userId;
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

    // DTOs pour les bulletins
    public class BulletinListeDto
    {
        public int Id { get; set; }
        public string Trimestre { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public long TailleFichier { get; set; }
        public int EnfantId { get; set; }
        public string EnfantNom { get; set; } = string.Empty;
        public string? ClasseNom { get; set; }
        public string ParentNom { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class BulletinDetailDto : BulletinListeDto
    {
        public DateTime EnfantDateNaissance { get; set; }
        public string? EnseignantPrincipalNom { get; set; }
        public string? ParentEmail { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateBulletinRequest
    {
        [Required(ErrorMessage = "Le trimestre est obligatoire")]
        public string Trimestre { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'année scolaire est obligatoire")]
        public string AnneeScolaire { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom du fichier est obligatoire")]
        public string NomFichier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le fichier est obligatoire")]
        public IFormFile Fichier { get; set; } = null!;
    }

    public class UpdateBulletinRequest
    {
        public string? Trimestre { get; set; }
        public string? AnneeScolaire { get; set; }
        public string? NomFichier { get; set; }
        public IFormFile? Fichier { get; set; }
    }

    public class BulletinStatistiquesDto
    {
        public int EnfantId { get; set; }
        public string EnfantNom { get; set; } = string.Empty;
        public string? ClasseNom { get; set; }
        public string ParentNom { get; set; } = string.Empty;
        public int NombreTotalBulletins { get; set; }
        public long TailleTotaleBulletins { get; set; }
        public List<AnneeScolaireBulletinStatDto> AnnesScolaires { get; set; } = new List<AnneeScolaireBulletinStatDto>();
    }

    public class AnneeScolaireBulletinStatDto
    {
        public string AnneeScolaire { get; set; } = string.Empty;
        public int NombreBulletins { get; set; }
        public int NombreTrimestres { get; set; }
        public long TailleTotal { get; set; }
    }
}