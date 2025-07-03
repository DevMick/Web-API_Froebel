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
    [Route("api/ecoles/{ecoleId}/enfants")]
    [ApiController]
    [Authorize]
    public class EnfantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EnfantController> _logger;

        public EnfantController(
            ApplicationDbContext context,
            ILogger<EnfantController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/enfants
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<EnfantListeDto>>> GetEnfants(
            int ecoleId,
            [FromQuery] int? classeId = null,
            [FromQuery] string? statut = null,
            [FromQuery] string? parentId = null,
            [FromQuery] bool? utiliseCantine = null,
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

                var query = _context.Enfants
                    .Where(e => e.EcoleId == ecoleId && !e.IsDeleted);

                // Filtrage par classe
                if (classeId.HasValue)
                {
                    query = query.Where(e => e.ClasseId == classeId.Value);
                }

                // Filtrage par statut
                if (!string.IsNullOrEmpty(statut))
                {
                    query = query.Where(e => e.Statut == statut);
                }

                // Filtrage par parent
                if (!string.IsNullOrEmpty(parentId))
                {
                    query = query.Where(e => e.ParentsEnfants.Any(pe => pe.ParentId == parentId));
                }

                // Filtrage par cantine
                if (utiliseCantine.HasValue)
                {
                    query = query.Where(e => e.UtiliseCantine == utiliseCantine.Value);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(e => e.Nom.ToLower().Contains(termeLower) ||
                                           e.Prenom.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var enfants = await query
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Include(e => e.Classe)
                    .Include(e => e.Bulletins.Where(b => !b.IsDeleted))
                    .Include(e => e.MessagesLiaison.Where(m => !m.IsDeleted))
                    .OrderBy(e => e.Nom)
                    .ThenBy(e => e.Prenom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var enfantsDtos = enfants.Select(e => new EnfantListeDto
                {
                    Id = e.Id,
                    Nom = e.Nom,
                    Prenom = e.Prenom,
                    DateNaissance = e.DateNaissance,
                    Sexe = e.Sexe,
                    Statut = e.Statut,
                    NumeroEtudiant = null, // Property removed from entity
                    ClasseId = e.ClasseId,
                    ClasseNom = e.Classe != null ? e.Classe.Nom : null,
                    ParentId = e.ParentsEnfants.FirstOrDefault()?.ParentId ?? string.Empty,
                    ParentNom = e.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty,
                    UtiliseCantine = e.UtiliseCantine,
                    CantinePaye = false, // Property removed from entity
                    NombreBulletins = e.Bulletins.Count,
                    NombreMessagesLiaison = e.MessagesLiaison.Count,
                    DateInscription = e.DateInscription,
                    CreatedAt = e.CreatedAt
                }).ToList();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(enfantsDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des enfants de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des enfants");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<EnfantDetailDto>> GetEnfant(int ecoleId, int id)
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
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEnfant(ecoleId, id))
                {
                    return Forbid("Accès non autorisé à cet enfant");
                }

                var enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Include(e => e.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(e => e.Bulletins.Where(b => !b.IsDeleted))
                    .Include(e => e.MessagesLiaison.Where(m => !m.IsDeleted))
                        .ThenInclude(m => m.CreatedBy)
                    .Include(e => e.Ecole)
                    .Where(e => e.Id == id && e.EcoleId == ecoleId && !e.IsDeleted)
                    .FirstOrDefaultAsync();

                if (enfant == null)
                {
                    return NotFound($"Enfant avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                var enfantDto = new EnfantDetailDto
                {
                    Id = enfant.Id,
                    Nom = enfant.Nom,
                    Prenom = enfant.Prenom,
                    DateNaissance = enfant.DateNaissance,
                    Sexe = enfant.Sexe,
                    Statut = enfant.Statut,
                    NumeroEtudiant = null, // Property removed from entity
                    DateInscription = enfant.DateInscription,
                    ClasseId = enfant.ClasseId,
                    ClasseNom = enfant.Classe?.Nom,
                    EnseignantPrincipalNom = enfant.Classe?.EnseignantPrincipal?.NomComplet,
                    ParentId = enfant.ParentsEnfants.FirstOrDefault()?.ParentId ?? string.Empty,
                    ParentNom = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty,
                    ParentTelephone = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.Telephone,
                    ParentEmail = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.Email,
                    UtiliseCantine = enfant.UtiliseCantine,
                    CantinePaye = false, // Property removed from entity
                    EcoleNom = enfant.Ecole?.Nom ?? string.Empty,
                    Bulletins = enfant.Bulletins.Select(b => new BulletinSimpleDto
                    {
                        Id = b.Id,
                        Trimestre = b.Trimestre,
                        AnneeScolaire = b.AnneeScolaire,
                        NomFichier = b.NomFichier,
                        CreatedAt = b.CreatedAt
                    }).OrderByDescending(b => b.CreatedAt).ToList(),
                    MessagesLiaison = enfant.MessagesLiaison.Select(m => new MessageLiaisonSimpleDto
                    {
                        Id = m.Id,
                        Titre = m.Titre,
                        Type = m.Type,
                        LuParParent = m.LuParParent,
                        ReponseRequise = m.ReponseRequise,
                        CreatedByNom = m.CreatedBy?.NomComplet ?? "Système",
                        CreatedAt = m.CreatedAt
                    }).OrderByDescending(m => m.CreatedAt).ToList(),
                    Paiements = new List<PaiementSimpleDto>(), // Paiements removed from entity
                    CreatedAt = enfant.CreatedAt,
                    UpdatedAt = enfant.UpdatedAt
                };

                return Ok(enfantDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'enfant {EnfantId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'enfant");
            }
        }

        // POST: api/ecoles/{ecoleId}/enfants
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Parent")]
        public async Task<ActionResult<EnfantDetailDto>> CreateEnfant(int ecoleId, [FromBody] CreateEnfantRequest request)
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
                if (!await CanManageEnfantInEcole(ecoleId, request.ParentId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de créer un enfant pour ce parent dans cette école");
                }

                // Vérifier que l'école existe
                var ecole = await _context.Ecoles.FindAsync(ecoleId);
                if (ecole == null || ecole.IsDeleted)
                {
                    return NotFound("École non trouvée");
                }

                // Vérifier que le parent existe et appartient à cette école
                var parent = await _context.Users
                    .Where(u => u.Id == request.ParentId &&
                              u.EcoleId == ecoleId)
                    .FirstOrDefaultAsync();

                if (parent == null)
                {
                    return BadRequest("Parent non trouvé dans cette école");
                }

                // Vérifier que l'utilisateur a le rôle Parent
                var isParent = await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .AnyAsync(x => x.UserId == request.ParentId && x.Name == "Parent");

                if (!isParent)
                {
                    return BadRequest("L'utilisateur spécifié n'a pas le rôle de parent");
                }

                // Vérifier la classe si spécifiée
                if (request.ClasseId.HasValue)
                {
                    var classe = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Id == request.ClasseId.Value &&
                                                c.EcoleId == ecoleId &&
                                                !c.IsDeleted);

                    if (classe == null)
                    {
                        return BadRequest("Classe non trouvée dans cette école");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var enfant = new Enfant
                {
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    DateNaissance = request.DateNaissance,
                    Sexe = request.Sexe,
                    ClasseId = request.ClasseId,
                    Statut = request.Statut ?? "pre_inscrit",
                    DateInscription = request.DateInscription ?? DateTime.UtcNow,
                    UtiliseCantine = request.UtiliseCantine,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId
                };

                _context.Enfants.Add(enfant);
                await _context.SaveChangesAsync();

                // Créer la liaison parent-enfant
                var parentEnfant = new ParentEnfant
                {
                    ParentId = request.ParentId,
                    EnfantId = enfant.Id,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId
                };
                _context.ParentEnfants.Add(parentEnfant);
                await _context.SaveChangesAsync();

                // Recharger l'enfant avec les relations
                enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Include(e => e.Classe)
                    .Include(e => e.Ecole)
                    .FirstAsync(e => e.Id == enfant.Id);

                _logger.LogInformation("Enfant '{Prenom} {Nom}' créé dans l'école {EcoleId} avec l'ID {Id}",
                    enfant.Prenom, enfant.Nom, ecoleId, enfant.Id);

                var result = new EnfantDetailDto
                {
                    Id = enfant.Id,
                    Nom = enfant.Nom,
                    Prenom = enfant.Prenom,
                    DateNaissance = enfant.DateNaissance,
                    Sexe = enfant.Sexe,
                    Statut = enfant.Statut,
                    NumeroEtudiant = null, // Property removed from entity
                    DateInscription = enfant.DateInscription,
                    ClasseId = enfant.ClasseId,
                    ClasseNom = enfant.Classe?.Nom,
                    ParentId = enfant.ParentsEnfants.FirstOrDefault()?.ParentId ?? string.Empty,
                    ParentNom = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty,
                    ParentTelephone = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.Telephone,
                    ParentEmail = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.Email,
                    UtiliseCantine = enfant.UtiliseCantine,
                    CantinePaye = false, // Property removed from entity
                    EcoleNom = enfant.Ecole?.Nom ?? string.Empty,
                    Bulletins = new List<BulletinSimpleDto>(),
                    MessagesLiaison = new List<MessageLiaisonSimpleDto>(),
                    Paiements = new List<PaiementSimpleDto>(),
                    CreatedAt = enfant.CreatedAt
                };

                return CreatedAtAction(nameof(GetEnfant), new { ecoleId, id = enfant.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'enfant dans l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'enfant");
            }
        }

        // PUT: api/ecoles/{ecoleId}/enfants/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateEnfant(int ecoleId, int id, [FromBody] UpdateEnfantRequest request)
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
                    return BadRequest("L'identifiant de l'enfant est invalide");
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

                var enfant = await _context.Enfants
                    .FirstOrDefaultAsync(e => e.Id == id && e.EcoleId == ecoleId && !e.IsDeleted);

                if (enfant == null)
                {
                    return NotFound($"Enfant avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                // Vérifier la classe si modifiée
                if (request.ClasseId.HasValue && request.ClasseId != enfant.ClasseId)
                {
                    var classe = await _context.Classes
                        .FirstOrDefaultAsync(c => c.Id == request.ClasseId.Value &&
                                                c.EcoleId == ecoleId &&
                                                !c.IsDeleted);

                    if (classe == null)
                    {
                        return BadRequest("Classe non trouvée dans cette école");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom))
                    enfant.Nom = request.Nom;
                if (!string.IsNullOrEmpty(request.Prenom))
                    enfant.Prenom = request.Prenom;
                if (request.DateNaissance.HasValue)
                    enfant.DateNaissance = request.DateNaissance.Value;
                if (!string.IsNullOrEmpty(request.Sexe))
                    enfant.Sexe = request.Sexe;
                if (!string.IsNullOrEmpty(request.Statut))
                    enfant.Statut = request.Statut;
                if (request.ClasseId != null)
                    enfant.ClasseId = request.ClasseId == 0 ? null : request.ClasseId;
                if (request.DateInscription.HasValue)
                    enfant.DateInscription = request.DateInscription;
                if (request.UtiliseCantine.HasValue)
                    enfant.UtiliseCantine = request.UtiliseCantine.Value;

                enfant.UpdatedAt = DateTime.UtcNow;
                enfant.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Enfant {Id} mis à jour dans l'école {EcoleId}", id, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'enfant {EnfantId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'enfant");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/enfants/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteEnfant(int ecoleId, int id)
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
                    return BadRequest("L'identifiant de l'enfant est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var enfant = await _context.Enfants
                    .FirstOrDefaultAsync(e => e.Id == id && e.EcoleId == ecoleId && !e.IsDeleted);

                if (enfant == null)
                {
                    return NotFound($"Enfant avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                enfant.IsDeleted = true;
                enfant.UpdatedAt = DateTime.UtcNow;
                enfant.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Enfant '{Prenom} {Nom}' supprimé (soft delete) de l'école {EcoleId}",
                    enfant.Prenom, enfant.Nom, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'enfant {EnfantId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'enfant");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<EnfantStatistiquesDto>> GetStatistiques(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var enfants = await _context.Enfants
                    .Where(e => e.EcoleId == ecoleId && !e.IsDeleted)
                    .ToListAsync();

                var statistiques = new EnfantStatistiquesDto
                {
                    EcoleId = ecoleId,
                    NombreTotalEnfants = enfants.Count,
                    NombreInscrits = enfants.Count(e => e.Statut == "inscrit"),
                    NombrePreInscrits = enfants.Count(e => e.Statut == "pre_inscrit"),
                    NombreSuspendus = enfants.Count(e => e.Statut == "suspendu"),
                    NombreDiplomes = enfants.Count(e => e.Statut == "diplome"),
                    NombreGarcons = enfants.Count(e => e.Sexe == "M"),
                    NombreFilles = enfants.Count(e => e.Sexe == "F"),
                    NombreUtiliseCantine = enfants.Count(e => e.UtiliseCantine),
                    NombreCantinePaye = 0, // Property removed from entity
                    NombreAvecClasse = enfants.Count(e => e.ClasseId.HasValue),
                    NombreSansClasse = enfants.Count(e => !e.ClasseId.HasValue)
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des enfants de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/export-excel
        [HttpGet("export-excel")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ExportEnfantsExcel(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var enfants = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Include(e => e.Classe)
                    .Where(e => e.EcoleId == ecoleId && !e.IsDeleted)
                    .OrderBy(e => e.Nom)
                    .ThenBy(e => e.Prenom)
                    .ToListAsync();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Enfants");

                // En-têtes
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nom";
                worksheet.Cells[1, 3].Value = "Prénom";
                worksheet.Cells[1, 4].Value = "Date Naissance";
                worksheet.Cells[1, 5].Value = "Sexe";
                worksheet.Cells[1, 6].Value = "Statut";
                worksheet.Cells[1, 7].Value = "Classe";
                worksheet.Cells[1, 8].Value = "Parent";
                worksheet.Cells[1, 9].Value = "Téléphone Parent";
                worksheet.Cells[1, 10].Value = "Cantine";
                worksheet.Cells[1, 11].Value = "Date Inscription";
                worksheet.Cells[1, 12].Value = "Date Création";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 12])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                for (int i = 0; i < enfants.Count; i++)
                {
                    var enfant = enfants[i];
                    var row = i + 2;

                    worksheet.Cells[row, 1].Value = enfant.Id;
                    worksheet.Cells[row, 2].Value = enfant.Nom;
                    worksheet.Cells[row, 3].Value = enfant.Prenom;
                    worksheet.Cells[row, 4].Value = enfant.DateNaissance.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 5].Value = enfant.Sexe;
                    worksheet.Cells[row, 6].Value = enfant.Statut;
                    worksheet.Cells[row, 7].Value = enfant.Classe?.Nom ?? "Non assigné";
                    worksheet.Cells[row, 8].Value = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? "Non trouvé";
                    worksheet.Cells[row, 9].Value = enfant.ParentsEnfants.FirstOrDefault()?.Parent?.Telephone ?? "";
                    worksheet.Cells[row, 10].Value = enfant.UtiliseCantine ? "Oui" : "Non";
                    worksheet.Cells[row, 11].Value = enfant.DateInscription?.ToString("dd/MM/yyyy") ?? "";
                    worksheet.Cells[row, 12].Value = enfant.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                }

                // Auto-fit des colonnes
                worksheet.Cells.AutoFitColumns();

                var ecole = await _context.Ecoles.FindAsync(ecoleId);
                var fileBytes = package.GetAsByteArray();
                var fileName = $"Enfants_{ecole?.Code}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Excel des enfants de l'école {EcoleId}", ecoleId);
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
                var enfant = await _context.ParentEnfants
                    .FirstOrDefaultAsync(pe => pe.EnfantId == enfantId && pe.ParentId == userId);
                return enfant != null;
            }

            return false;
        }

        private async Task<bool> CanManageEnfantInEcole(int ecoleId, string parentId)
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

            if (User.IsInRole("Parent"))
                return userId == parentId;

            return false;
        }
    }

    // DTOs pour les enfants
    public class EnfantListeDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string? NumeroEtudiant { get; set; }
        public int? ClasseId { get; set; }
        public string? ClasseNom { get; set; }
        public string ParentId { get; set; } = string.Empty;
        public string ParentNom { get; set; } = string.Empty;
        public bool UtiliseCantine { get; set; }
        public bool CantinePaye { get; set; }
        public int NombreBulletins { get; set; }
        public int NombreMessagesLiaison { get; set; }
        public DateTime? DateInscription { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EnfantDetailDto : EnfantListeDto
    {
        public string? EnseignantPrincipalNom { get; set; }
        public string? ParentTelephone { get; set; }
        public string? ParentEmail { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public List<BulletinSimpleDto> Bulletins { get; set; } = new List<BulletinSimpleDto>();
        public List<MessageLiaisonSimpleDto> MessagesLiaison { get; set; } = new List<MessageLiaisonSimpleDto>();
        public List<PaiementSimpleDto> Paiements { get; set; } = new List<PaiementSimpleDto>();
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateEnfantRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date de naissance est obligatoire")]
        public DateTime DateNaissance { get; set; }

        [Required(ErrorMessage = "Le sexe est obligatoire")]
        public string Sexe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le parent est obligatoire")]
        public string ParentId { get; set; } = string.Empty;

        public int? ClasseId { get; set; }
        public string? NumeroEtudiant { get; set; }
        public string? Statut { get; set; }
        public DateTime? DateInscription { get; set; }
        public bool UtiliseCantine { get; set; } = false;
        public bool CantinePaye { get; set; } = false;
    }

    public class UpdateEnfantRequest
    {
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public DateTime? DateNaissance { get; set; }
        public string? Sexe { get; set; }
        public string? Statut { get; set; }
        public string? NumeroEtudiant { get; set; }
        public int? ClasseId { get; set; }
        public DateTime? DateInscription { get; set; }
        public bool? UtiliseCantine { get; set; }
        public bool? CantinePaye { get; set; }
    }

    public class EnfantStatistiquesDto
    {
        public int EcoleId { get; set; }
        public int NombreTotalEnfants { get; set; }
        public int NombreInscrits { get; set; }
        public int NombrePreInscrits { get; set; }
        public int NombreSuspendus { get; set; }
        public int NombreDiplomes { get; set; }
        public int NombreGarcons { get; set; }
        public int NombreFilles { get; set; }
        public int NombreUtiliseCantine { get; set; }
        public int NombreCantinePaye { get; set; }
        public int NombreAvecClasse { get; set; }
        public int NombreSansClasse { get; set; }
    }

    public class BulletinSimpleDto
    {
        public int Id { get; set; }
        public string Trimestre { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class MessageLiaisonSimpleDto
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool LuParParent { get; set; }
        public bool ReponseRequise { get; set; }
        public string CreatedByNom { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PaiementSimpleDto
    {
        public int Id { get; set; }
        public string TypePaiement { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DatePaiement { get; set; }
        public DateTime DateEcheance { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Trimestre { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
    }
}