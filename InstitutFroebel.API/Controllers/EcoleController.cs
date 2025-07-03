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
    [Route("api/ecoles")]
    [ApiController]
    public class EcoleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EcoleController> _logger;

        public EcoleController(
            ApplicationDbContext context,
            ILogger<EcoleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EcoleListeDto>>> GetEcoles(
            [FromQuery] string? recherche = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Ecoles
                    .Where(e => !e.IsDeleted);

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(e => e.Nom.ToLower().Contains(termeLower) ||
                                           e.Code.ToLower().Contains(termeLower) ||
                                           e.Commune.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var ecoles = await query
                    .OrderBy(e => e.Nom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(e => e.Users)
                    .Include(e => e.Classes)
                    .Include(e => e.Enfants)
                    .Select(e => new EcoleListeDto
                    {
                        Id = e.Id,
                        Nom = e.Nom,
                        Code = e.Code,
                        Adresse = e.Adresse,
                        Commune = e.Commune,
                        Telephone = e.Telephone,
                        Email = e.Email,
                        AnneeScolaire = e.AnneeScolaire,
                        NombreUtilisateurs = e.Users.Count(),
                        NombreClasses = e.Classes.Count(c => !c.IsDeleted),
                        NombreEleves = e.Enfants.Count(en => !en.IsDeleted),
                        CreatedAt = e.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(ecoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des écoles");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des écoles");
            }
        }

        // GET: api/ecoles/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EcoleDetailDto>> GetEcole(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                var ecole = await _context.Ecoles
                    .Include(e => e.Users)
                    .Include(e => e.Classes)
                    .Include(e => e.Enfants)
                    .Include(e => e.Annonces)
                    .Include(e => e.Activites)
                    .Where(e => e.Id == id && !e.IsDeleted)
                    .Select(e => new EcoleDetailDto
                    {
                        Id = e.Id,
                        Nom = e.Nom,
                        Code = e.Code,
                        Adresse = e.Adresse,
                        Commune = e.Commune,
                        Telephone = e.Telephone,
                        Email = e.Email,
                        AnneeScolaire = e.AnneeScolaire,
                        NombreUtilisateurs = e.Users.Count(),
                        NombreClasses = e.Classes.Count(c => !c.IsDeleted),
                        NombreEleves = e.Enfants.Count(en => !en.IsDeleted),
                        NombreAnnonces = e.Annonces.Count(a => !a.IsDeleted),
                        NombreActivites = e.Activites.Count(a => !a.IsDeleted),
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (ecole == null)
                {
                    return NotFound($"École avec l'ID {id} non trouvée");
                }

                return Ok(ecole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'école {EcoleId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'école");
            }
        }

        // POST: api/ecoles
        [HttpPost]
        public async Task<ActionResult<EcoleDetailDto>> CreateEcole([FromBody] CreateEcoleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier l'unicité du code
                var existingCode = await _context.Ecoles
                    .AnyAsync(e => e.Code.ToLower() == request.Code.ToLower() && !e.IsDeleted);

                if (existingCode)
                {
                    return BadRequest($"Une école avec le code '{request.Code}' existe déjà");
                }

                // Vérifier l'unicité de l'email
                var existingEmail = await _context.Ecoles
                    .AnyAsync(e => e.Email.ToLower() == request.Email.ToLower() && !e.IsDeleted);

                if (existingEmail)
                {
                    return BadRequest($"Une école avec l'email '{request.Email}' existe déjà");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var ecole = new Ecole
                {
                    Nom = request.Nom,
                    Code = request.Code.ToUpper(),
                    Adresse = request.Adresse,
                    Commune = request.Commune,
                    Telephone = request.Telephone,
                    Email = request.Email.ToLower(),
                    AnneeScolaire = request.AnneeScolaire,
                    CreatedById = currentUserId
                };

                _context.Ecoles.Add(ecole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("École '{Nom}' créée avec le code '{Code}' et l'ID {Id}",
                    ecole.Nom, ecole.Code, ecole.Id);

                var result = new EcoleDetailDto
                {
                    Id = ecole.Id,
                    Nom = ecole.Nom,
                    Code = ecole.Code,
                    Adresse = ecole.Adresse,
                    Commune = ecole.Commune,
                    Telephone = ecole.Telephone,
                    Email = ecole.Email,
                    AnneeScolaire = ecole.AnneeScolaire,
                    NombreUtilisateurs = 0,
                    NombreClasses = 0,
                    NombreEleves = 0,
                    NombreAnnonces = 0,
                    NombreActivites = 0,
                    CreatedAt = ecole.CreatedAt
                };

                return CreatedAtAction(nameof(GetEcole), new { id = ecole.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'école '{Nom}'", request.Nom);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'école");
            }
        }

        // PUT: api/ecoles/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEcole(int id, [FromBody] UpdateEcoleRequest request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var ecole = await _context.Ecoles
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

                if (ecole == null)
                {
                    return NotFound($"École avec l'ID {id} non trouvée");
                }

                // Vérifier l'unicité du code si modifié
                if (!string.IsNullOrEmpty(request.Code) &&
                    request.Code.ToUpper() != ecole.Code.ToUpper())
                {
                    var existingCode = await _context.Ecoles
                        .AnyAsync(e => e.Id != id &&
                                     e.Code.ToLower() == request.Code.ToLower() &&
                                     !e.IsDeleted);

                    if (existingCode)
                    {
                        return BadRequest($"Une école avec le code '{request.Code}' existe déjà");
                    }
                }

                // Vérifier l'unicité de l'email si modifié
                if (!string.IsNullOrEmpty(request.Email) &&
                    request.Email.ToLower() != ecole.Email.ToLower())
                {
                    var existingEmail = await _context.Ecoles
                        .AnyAsync(e => e.Id != id &&
                                     e.Email.ToLower() == request.Email.ToLower() &&
                                     !e.IsDeleted);

                    if (existingEmail)
                    {
                        return BadRequest($"Une école avec l'email '{request.Email}' existe déjà");
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom))
                    ecole.Nom = request.Nom;
                if (!string.IsNullOrEmpty(request.Code))
                    ecole.Code = request.Code.ToUpper();
                if (!string.IsNullOrEmpty(request.Adresse))
                    ecole.Adresse = request.Adresse;
                if (!string.IsNullOrEmpty(request.Commune))
                    ecole.Commune = request.Commune;
                if (!string.IsNullOrEmpty(request.Telephone))
                    ecole.Telephone = request.Telephone;
                if (!string.IsNullOrEmpty(request.Email))
                    ecole.Email = request.Email.ToLower();
                if (!string.IsNullOrEmpty(request.AnneeScolaire))
                    ecole.AnneeScolaire = request.AnneeScolaire;

                ecole.UpdatedAt = DateTime.UtcNow;
                ecole.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("École {Id} mise à jour", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'école {EcoleId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'école");
            }
        }

        // DELETE: api/ecoles/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEcole(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                var ecole = await _context.Ecoles
                    .Include(e => e.Users)
                    .Include(e => e.Enfants)
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

                if (ecole == null)
                {
                    return NotFound($"École avec l'ID {id} non trouvée");
                }

                // Vérifier s'il y a des utilisateurs ou élèves actifs
                var hasActiveUsers = ecole.Users.Any();
                var hasActiveStudents = ecole.Enfants.Any(e => !e.IsDeleted);

                if (hasActiveUsers || hasActiveStudents)
                {
                    return BadRequest("Impossible de supprimer une école qui contient des utilisateurs ou des élèves actifs");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                ecole.IsDeleted = true;
                ecole.UpdatedAt = DateTime.UtcNow;
                ecole.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("École '{Nom}' supprimée (soft delete)", ecole.Nom);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'école {EcoleId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'école");
            }
        }

        // GET: api/ecoles/{id}/statistiques
        [HttpGet("{id:int}/statistiques")]
        public async Task<ActionResult<EcoleStatistiquesDto>> GetStatistiques(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                var ecole = await _context.Ecoles
                    .Where(e => e.Id == id && !e.IsDeleted)
                    .FirstOrDefaultAsync();

                if (ecole == null)
                {
                    return NotFound($"École avec l'ID {id} non trouvée");
                }

                var statistiques = new EcoleStatistiquesDto
                {
                    EcoleId = id,
                    EcoleNom = ecole.Nom,
                    AnneeScolaire = ecole.AnneeScolaire
                };

                // Statistiques utilisateurs
                var users = await _context.Users
                    .Where(u => u.EcoleId == id)
                    .ToListAsync();

                statistiques.NombreTotalUtilisateurs = users.Count;
                statistiques.NombreAdmins = users.Count(u => _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Any(x => x.UserId == u.Id && x.Name == "Admin"));
                statistiques.NombreEnseignants = users.Count(u => _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Any(x => x.UserId == u.Id && x.Name == "Teacher"));
                statistiques.NombreParents = users.Count(u => _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Any(x => x.UserId == u.Id && x.Name == "Parent"));

                // Statistiques classes
                statistiques.NombreClasses = await _context.Classes
                    .Where(c => c.EcoleId == id && !c.IsDeleted)
                    .CountAsync();

                // Statistiques élèves
                var eleves = await _context.Enfants
                    .Where(e => e.EcoleId == id && !e.IsDeleted)
                    .ToListAsync();

                statistiques.NombreTotalEleves = eleves.Count;
                statistiques.NombreElevesInscrits = eleves.Count(e => e.Statut == "inscrit");
                statistiques.NombreElevesPreInscrits = eleves.Count(e => e.Statut == "pre_inscrit");
                statistiques.NombreElevesCantine = eleves.Count(e => e.UtiliseCantine);

                // Statistiques communications
                statistiques.NombreAnnonces = await _context.Annonces
                    .Where(a => a.EcoleId == id && !a.IsDeleted)
                    .CountAsync();

                statistiques.NombreActivites = await _context.Activites
                    .Where(a => a.EcoleId == id && !a.IsDeleted)
                    .CountAsync();

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de l'école {EcoleId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/ecoles/export-excel
        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportEcolesExcel()
        {
            try
            {
                var ecoles = await _context.Ecoles
                    .Where(e => !e.IsDeleted)
                    .Include(e => e.Users)
                    .Include(e => e.Classes)
                    .Include(e => e.Enfants)
                    .OrderBy(e => e.Nom)
                    .ToListAsync();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Écoles");

                // En-têtes
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nom";
                worksheet.Cells[1, 3].Value = "Code";
                worksheet.Cells[1, 4].Value = "Adresse";
                worksheet.Cells[1, 5].Value = "Commune";
                worksheet.Cells[1, 6].Value = "Téléphone";
                worksheet.Cells[1, 7].Value = "Email";
                worksheet.Cells[1, 8].Value = "Année Scolaire";
                worksheet.Cells[1, 9].Value = "Nb Utilisateurs";
                worksheet.Cells[1, 10].Value = "Nb Classes";
                worksheet.Cells[1, 11].Value = "Nb Élèves";
                worksheet.Cells[1, 12].Value = "Date Création";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 12])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                for (int i = 0; i < ecoles.Count; i++)
                {
                    var ecole = ecoles[i];
                    var row = i + 2;

                    worksheet.Cells[row, 1].Value = ecole.Id;
                    worksheet.Cells[row, 2].Value = ecole.Nom;
                    worksheet.Cells[row, 3].Value = ecole.Code;
                    worksheet.Cells[row, 4].Value = ecole.Adresse;
                    worksheet.Cells[row, 5].Value = ecole.Commune;
                    worksheet.Cells[row, 6].Value = ecole.Telephone;
                    worksheet.Cells[row, 7].Value = ecole.Email;
                    worksheet.Cells[row, 8].Value = ecole.AnneeScolaire;
                    worksheet.Cells[row, 9].Value = ecole.Users.Count();
                    worksheet.Cells[row, 10].Value = ecole.Classes.Count(c => !c.IsDeleted);
                    worksheet.Cells[row, 11].Value = ecole.Enfants.Count(e => !e.IsDeleted);
                    worksheet.Cells[row, 12].Value = ecole.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                }

                // Auto-fit des colonnes
                worksheet.Cells.AutoFitColumns();

                var fileBytes = package.GetAsByteArray();
                var fileName = $"Ecoles_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Excel des écoles");
                return StatusCode(500, "Une erreur est survenue lors de l'export Excel");
            }
        }
    }

    // DTOs pour les écoles
    public class EcoleListeDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Commune { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public int NombreUtilisateurs { get; set; }
        public int NombreClasses { get; set; }
        public int NombreEleves { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EcoleDetailDto : EcoleListeDto
    {
        public int NombreAnnonces { get; set; }
        public int NombreActivites { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateEcoleRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code est obligatoire")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse est obligatoire")]
        public string Adresse { get; set; } = string.Empty;

        [Required(ErrorMessage = "La commune est obligatoire")]
        public string Commune { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est obligatoire")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'année scolaire est obligatoire")]
        public string AnneeScolaire { get; set; } = string.Empty;
    }

    public class UpdateEcoleRequest
    {
        public string? Nom { get; set; }
        public string? Code { get; set; }
        public string? Adresse { get; set; }
        public string? Commune { get; set; }
        public string? Telephone { get; set; }

        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string? Email { get; set; }

        public string? AnneeScolaire { get; set; }
    }

    public class EcoleStatistiquesDto
    {
        public int EcoleId { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public int NombreTotalUtilisateurs { get; set; }
        public int NombreAdmins { get; set; }
        public int NombreEnseignants { get; set; }
        public int NombreParents { get; set; }
        public int NombreClasses { get; set; }
        public int NombreTotalEleves { get; set; }
        public int NombreElevesInscrits { get; set; }
        public int NombreElevesPreInscrits { get; set; }
        public int NombreElevesCantine { get; set; }
        public int NombreAnnonces { get; set; }
        public int NombreActivites { get; set; }
    }
}