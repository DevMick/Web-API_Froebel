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
    [Route("api/ecoles/{ecoleId}/classes")]
    [ApiController]
    [Authorize]
    public class ClasseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClasseController> _logger;

        public ClasseController(
            ApplicationDbContext context,
            ILogger<ClasseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/classes
        [HttpGet]
        [AllowAnonymous] // Permet l'accès sans authentification
        public async Task<ActionResult<IEnumerable<ClasseListeDto>>> GetClasses(
            int ecoleId,
            [FromQuery] string? recherche = null,
            [FromQuery] string? enseignantId = null,
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

                // Vérifier que l'école existe
                var ecoleExists = await _context.Ecoles
                    .AnyAsync(e => e.Id == ecoleId && !e.IsDeleted);

                if (!ecoleExists)
                {
                    return NotFound("École non trouvée");
                }

                var query = _context.Classes
                    .Where(c => c.EcoleId == ecoleId && !c.IsDeleted);

                // Filtrage par enseignant
                if (!string.IsNullOrEmpty(enseignantId))
                {
                    query = query.Where(c => c.EnseignantPrincipalId == enseignantId);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(c => c.Nom.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var classes = await query
                    .Include(c => c.EnseignantPrincipal)
                    .Include(c => c.Enfants.Where(e => !e.IsDeleted))
                    .Include(c => c.EmploisDuTemps.Where(e => !e.IsDeleted))
                    .OrderBy(c => c.Nom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new ClasseListeDto
                    {
                        Id = c.Id,
                        Nom = c.Nom,
                        Effectif = c.Effectif,
                        EnseignantPrincipalId = c.EnseignantPrincipalId,
                        EnseignantPrincipalNom = c.EnseignantPrincipal != null ? c.EnseignantPrincipal.NomComplet : null,
                        NombreEleves = c.Enfants.Count,
                        NombreEmploisDuTemps = c.EmploisDuTemps.Count,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(classes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des classes de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des classes");
            }
        }

        // GET: api/ecoles/{ecoleId}/classes/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<ClasseDetailDto>> GetClasse(int ecoleId, int id)
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
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var classe = await _context.Classes
                    .Include(c => c.EnseignantPrincipal)
                    .Include(c => c.Enfants.Where(e => !e.IsDeleted))
                        .ThenInclude(e => e.ParentsEnfants)
                            .ThenInclude(pe => pe.Parent)
                    .Include(c => c.EmploisDuTemps.Where(e => !e.IsDeleted))
                    .Include(c => c.Ecole)
                    .Where(c => c.Id == id && c.EcoleId == ecoleId && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (classe == null)
                {
                    return NotFound($"Classe avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                var classeDto = new ClasseDetailDto
                {
                    Id = classe.Id,
                    Nom = classe.Nom,
                    Effectif = classe.Effectif,
                    EnseignantPrincipalId = classe.EnseignantPrincipalId,
                    EnseignantPrincipalNom = classe.EnseignantPrincipal?.NomComplet,
                    EcoleNom = classe.Ecole?.Nom ?? string.Empty,
                    Eleves = classe.Enfants.Select(e => new EleveSimpleDto
                    {
                        Id = e.Id,
                        Nom = e.Nom,
                        Prenom = e.Prenom,
                        DateNaissance = e.DateNaissance,
                        Sexe = e.Sexe,
                        Statut = e.Statut,
                        ParentNom = e.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty
                    }).OrderBy(e => e.Nom).ThenBy(e => e.Prenom).ToList(),
                    EmploisDuTemps = classe.EmploisDuTemps.Select(e => new EmploiSimpleDto
                    {
                        Id = e.Id,
                        NomFichier = e.NomFichier,
                        AnneeScolaire = e.AnneeScolaire,
                        CreatedAt = e.CreatedAt
                    }).OrderByDescending(e => e.CreatedAt).ToList(),
                    CreatedAt = classe.CreatedAt,
                    UpdatedAt = classe.UpdatedAt
                };

                return Ok(classeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la classe {ClasseId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la classe");
            }
        }

        // POST: api/ecoles/{ecoleId}/classes
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ClasseDetailDto>> CreateClasse(int ecoleId, [FromBody] CreateClasseRequest request)
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

                // Vérifier l'unicité du nom de classe dans l'école
                var existingClasse = await _context.Classes
                    .AnyAsync(c => c.Nom.ToLower() == request.Nom.ToLower() &&
                                 c.EcoleId == ecoleId &&
                                 !c.IsDeleted);

                if (existingClasse)
                {
                    return BadRequest($"Une classe avec le nom '{request.Nom}' existe déjà dans cette école");
                }

                // Vérifier que l'enseignant existe et appartient à cette école (si spécifié)
                if (!string.IsNullOrEmpty(request.EnseignantPrincipalId))
                {
                    var enseignant = await _context.Users
                        .Where(u => u.Id == request.EnseignantPrincipalId &&
                                  u.EcoleId == ecoleId)
                        .FirstOrDefaultAsync();

                    if (enseignant == null)
                    {
                        return BadRequest("Enseignant non trouvé dans cette école");
                    }

                    // Vérifier que l'utilisateur a le rôle Teacher
                    var isTeacher = await _context.UserRoles
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                        .AnyAsync(x => x.UserId == request.EnseignantPrincipalId && x.Name == "Teacher");

                    if (!isTeacher)
                    {
                        return BadRequest("L'utilisateur spécifié n'a pas le rôle d'enseignant");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var classe = new Classe
                {
                    Nom = request.Nom,
                    Effectif = request.Effectif,
                    EnseignantPrincipalId = request.EnseignantPrincipalId,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId
                };

                _context.Classes.Add(classe);
                await _context.SaveChangesAsync();

                // Recharger la classe avec les relations
                classe = await _context.Classes
                    .Include(c => c.EnseignantPrincipal)
                    .Include(c => c.Ecole)
                    .FirstAsync(c => c.Id == classe.Id);

                _logger.LogInformation("Classe '{Nom}' créée dans l'école {EcoleId} avec l'ID {Id}",
                    classe.Nom, ecoleId, classe.Id);

                var result = new ClasseDetailDto
                {
                    Id = classe.Id,
                    Nom = classe.Nom,
                    Effectif = classe.Effectif,
                    EnseignantPrincipalId = classe.EnseignantPrincipalId,
                    EnseignantPrincipalNom = classe.EnseignantPrincipal?.NomComplet,
                    EcoleNom = classe.Ecole?.Nom ?? string.Empty,
                    Eleves = new List<EleveSimpleDto>(),
                    EmploisDuTemps = new List<EmploiSimpleDto>(),
                    CreatedAt = classe.CreatedAt
                };

                return CreatedAtAction(nameof(GetClasse), new { ecoleId, id = classe.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la classe dans l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la classe");
            }
        }

        // PUT: api/ecoles/{ecoleId}/classes/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateClasse(int ecoleId, int id, [FromBody] UpdateClasseRequest request)
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
                    return BadRequest("L'identifiant de la classe est invalide");
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

                var classe = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Id == id && c.EcoleId == ecoleId && !c.IsDeleted);

                if (classe == null)
                {
                    return NotFound($"Classe avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                // Vérifier l'unicité du nom si modifié
                if (!string.IsNullOrEmpty(request.Nom) &&
                    request.Nom.ToLower() != classe.Nom.ToLower())
                {
                    var existingNom = await _context.Classes
                        .AnyAsync(c => c.Id != id &&
                                     c.Nom.ToLower() == request.Nom.ToLower() &&
                                     c.EcoleId == ecoleId &&
                                     !c.IsDeleted);

                    if (existingNom)
                    {
                        return BadRequest($"Une classe avec le nom '{request.Nom}' existe déjà dans cette école");
                    }
                }

                // Vérifier l'enseignant si modifié
                if (!string.IsNullOrEmpty(request.EnseignantPrincipalId) &&
                    request.EnseignantPrincipalId != classe.EnseignantPrincipalId)
                {
                    var enseignant = await _context.Users
                        .Where(u => u.Id == request.EnseignantPrincipalId &&
                                  u.EcoleId == ecoleId)
                        .FirstOrDefaultAsync();

                    if (enseignant == null)
                    {
                        return BadRequest("Enseignant non trouvé dans cette école");
                    }

                    var isTeacher = await _context.UserRoles
                        .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                        .AnyAsync(x => x.UserId == request.EnseignantPrincipalId && x.Name == "Teacher");

                    if (!isTeacher)
                    {
                        return BadRequest("L'utilisateur spécifié n'a pas le rôle d'enseignant");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom))
                    classe.Nom = request.Nom;
                if (request.Effectif.HasValue)
                    classe.Effectif = request.Effectif.Value;
                if (request.EnseignantPrincipalId != null)
                    classe.EnseignantPrincipalId = string.IsNullOrEmpty(request.EnseignantPrincipalId) ? null : request.EnseignantPrincipalId;

                classe.UpdatedAt = DateTime.UtcNow;
                classe.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Classe {Id} mise à jour dans l'école {EcoleId}", id, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la classe {ClasseId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la classe");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/classes/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteClasse(int ecoleId, int id)
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
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var classe = await _context.Classes
                    .Include(c => c.Enfants)
                    .FirstOrDefaultAsync(c => c.Id == id && c.EcoleId == ecoleId && !c.IsDeleted);

                if (classe == null)
                {
                    return NotFound($"Classe avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                // Vérifier s'il y a des élèves associés
                var hasActiveStudents = classe.Enfants.Any(e => !e.IsDeleted);
                if (hasActiveStudents)
                {
                    return BadRequest("Impossible de supprimer une classe qui contient des élèves");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                classe.IsDeleted = true;
                classe.UpdatedAt = DateTime.UtcNow;
                classe.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Classe '{Nom}' supprimée (soft delete) de l'école {EcoleId}", classe.Nom, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la classe {ClasseId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la classe");
            }
        }

        // GET: api/ecoles/{ecoleId}/classes/{id}/eleves
        [HttpGet("{id:int}/eleves")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<IEnumerable<EleveSimpleDto>>> GetElevesClasse(int ecoleId, int id)
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
                    return BadRequest("L'identifiant de la classe est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var classe = await _context.Classes
                    .FirstOrDefaultAsync(c => c.Id == id && c.EcoleId == ecoleId && !c.IsDeleted);

                if (classe == null)
                {
                    return NotFound($"Classe avec l'ID {id} non trouvée dans l'école {ecoleId}");
                }

                var enfants = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Where(e => e.ClasseId == id && !e.IsDeleted)
                    .OrderBy(e => e.Nom)
                    .ThenBy(e => e.Prenom)
                    .ToListAsync();

                var eleves = enfants.Select(e => new EleveSimpleDto
                {
                    Id = e.Id,
                    Nom = e.Nom,
                    Prenom = e.Prenom,
                    DateNaissance = e.DateNaissance,
                    Sexe = e.Sexe,
                    Statut = e.Statut,
                    ParentNom = e.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty
                }).ToList();

                return Ok(eleves);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des élèves de la classe {ClasseId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des élèves");
            }
        }

        // GET: api/ecoles/{ecoleId}/classes/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ClasseStatistiquesDto>> GetStatistiques(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var classes = await _context.Classes
                    .Include(c => c.Enfants.Where(e => !e.IsDeleted))
                    .Include(c => c.EnseignantPrincipal)
                    .Where(c => c.EcoleId == ecoleId && !c.IsDeleted)
                    .ToListAsync();

                var statistiques = new ClasseStatistiquesDto
                {
                    EcoleId = ecoleId,
                    NombreTotalClasses = classes.Count,
                    NombreClassesAvecEnseignant = classes.Count(c => c.EnseignantPrincipalId != null),
                    NombreClassesSansEnseignant = classes.Count(c => c.EnseignantPrincipalId == null),
                    EffectifTotalPrevu = classes.Sum(c => c.Effectif),
                    EffectifTotalReel = classes.Sum(c => c.Enfants.Count),
                    TauxOccupationMoyen = classes.Any() ?
                        classes.Where(c => c.Effectif > 0).Average(c => (double)c.Enfants.Count / c.Effectif * 100) : 0
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des classes de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/ecoles/{ecoleId}/classes/export-excel
        [HttpGet("export-excel")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ExportClassesExcel(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var classes = await _context.Classes
                    .Include(c => c.EnseignantPrincipal)
                    .Include(c => c.Enfants.Where(e => !e.IsDeleted))
                    .Include(c => c.EmploisDuTemps.Where(e => !e.IsDeleted))
                    .Where(c => c.EcoleId == ecoleId && !c.IsDeleted)
                    .OrderBy(c => c.Nom)
                    .ToListAsync();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Classes");

                // En-têtes
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nom";
                worksheet.Cells[1, 3].Value = "Effectif Prévu";
                worksheet.Cells[1, 4].Value = "Effectif Réel";
                worksheet.Cells[1, 5].Value = "Taux Occupation (%)";
                worksheet.Cells[1, 6].Value = "Enseignant Principal";
                worksheet.Cells[1, 7].Value = "Nb Emplois du Temps";
                worksheet.Cells[1, 8].Value = "Date Création";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                for (int i = 0; i < classes.Count; i++)
                {
                    var classe = classes[i];
                    var row = i + 2;
                    var effectifReel = classe.Enfants.Count;
                    var tauxOccupation = classe.Effectif > 0 ? (double)effectifReel / classe.Effectif * 100 : 0;

                    worksheet.Cells[row, 1].Value = classe.Id;
                    worksheet.Cells[row, 2].Value = classe.Nom;
                    worksheet.Cells[row, 3].Value = classe.Effectif;
                    worksheet.Cells[row, 4].Value = effectifReel;
                    worksheet.Cells[row, 5].Value = Math.Round(tauxOccupation, 1);
                    worksheet.Cells[row, 6].Value = classe.EnseignantPrincipal?.NomComplet ?? "Non assigné";
                    worksheet.Cells[row, 7].Value = classe.EmploisDuTemps.Count;
                    worksheet.Cells[row, 8].Value = classe.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                }

                // Auto-fit des colonnes
                worksheet.Cells.AutoFitColumns();

                var ecole = await _context.Ecoles.FindAsync(ecoleId);
                var fileBytes = package.GetAsByteArray();
                var fileName = $"Classes_{ecole?.Code}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Excel des classes de l'école {EcoleId}", ecoleId);
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

    // DTOs pour les classes
    public class ClasseListeDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int Effectif { get; set; }
        public string? EnseignantPrincipalId { get; set; }
        public string? EnseignantPrincipalNom { get; set; }
        public int NombreEleves { get; set; }
        public int NombreEmploisDuTemps { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ClasseDetailDto : ClasseListeDto
    {
        public string EcoleNom { get; set; } = string.Empty;
        public List<EleveSimpleDto> Eleves { get; set; } = new List<EleveSimpleDto>();
        public List<EmploiSimpleDto> EmploisDuTemps { get; set; } = new List<EmploiSimpleDto>();
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateClasseRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'effectif est obligatoire")]
        [Range(1, 100, ErrorMessage = "L'effectif doit être entre 1 et 100")]
        public int Effectif { get; set; }

        public string? EnseignantPrincipalId { get; set; }
    }

    public class UpdateClasseRequest
    {
        public string? Nom { get; set; }

        [Range(1, 100, ErrorMessage = "L'effectif doit être entre 1 et 100")]
        public int? Effectif { get; set; }

        public string? EnseignantPrincipalId { get; set; }
    }

    public class ClasseStatistiquesDto
    {
        public int EcoleId { get; set; }
        public int NombreTotalClasses { get; set; }
        public int NombreClassesAvecEnseignant { get; set; }
        public int NombreClassesSansEnseignant { get; set; }
        public int EffectifTotalPrevu { get; set; }
        public int EffectifTotalReel { get; set; }
        public double TauxOccupationMoyen { get; set; }
    }

    public class EleveSimpleDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string ParentNom { get; set; } = string.Empty;
    }

    public class EmploiSimpleDto
    {
        public int Id { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}