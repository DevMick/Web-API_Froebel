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
    [Route("api/ecoles/{ecoleId}/paiements")]
    [ApiController]
    [Authorize]
    public class PaiementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaiementController> _logger;

        public PaiementController(
            ApplicationDbContext context,
            ILogger<PaiementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/paiements
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<IEnumerable<PaiementListeDto>>> GetPaiements(
            int ecoleId,
            [FromQuery] int? enfantId = null,
            [FromQuery] string? typePaiement = null,
            [FromQuery] string? statut = null,
            [FromQuery] string? modePaiement = null,
            [FromQuery] string? trimestre = null,
            [FromQuery] string? anneeScolaire = null,
            [FromQuery] DateTime? dateDebutEcheance = null,
            [FromQuery] DateTime? dateFinEcheance = null,
            [FromQuery] DateTime? dateDebutPaiement = null,
            [FromQuery] DateTime? dateFinPaiement = null,
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

                var query = _context.Paiements
                    .Where(p => p.EcoleId == ecoleId && !p.IsDeleted);

                // Filtrage par enfant
                if (enfantId.HasValue)
                {
                    query = query.Where(p => p.EnfantId == enfantId.Value);
                }

                // Filtrage par type de paiement
                if (!string.IsNullOrEmpty(typePaiement))
                {
                    query = query.Where(p => p.TypePaiement == typePaiement);
                }

                // Filtrage par statut
                if (!string.IsNullOrEmpty(statut))
                {
                    query = query.Where(p => p.Statut == statut);
                }

                // Filtrage par mode de paiement
                if (!string.IsNullOrEmpty(modePaiement))
                {
                    query = query.Where(p => p.ModePaiement == modePaiement);
                }

                // Filtrage par trimestre
                if (!string.IsNullOrEmpty(trimestre))
                {
                    query = query.Where(p => p.Trimestre == trimestre);
                }

                // Filtrage par année scolaire
                if (!string.IsNullOrEmpty(anneeScolaire))
                {
                    query = query.Where(p => p.AnneeScolaire == anneeScolaire);
                }

                // Filtrage par période d'échéance
                if (dateDebutEcheance.HasValue)
                {
                    query = query.Where(p => p.DateEcheance >= dateDebutEcheance.Value);
                }
                if (dateFinEcheance.HasValue)
                {
                    query = query.Where(p => p.DateEcheance <= dateFinEcheance.Value);
                }

                // Filtrage par période de paiement
                if (dateDebutPaiement.HasValue)
                {
                    query = query.Where(p => p.DatePaiement >= dateDebutPaiement.Value);
                }
                if (dateFinPaiement.HasValue)
                {
                    query = query.Where(p => p.DatePaiement <= dateFinPaiement.Value);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(p => (p.Description != null && p.Description.ToLower().Contains(termeLower)) ||
                                           (p.NumeroPiece != null && p.NumeroPiece.ToLower().Contains(termeLower)));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var paiements = await query
                    .Include(p => p.Enfant)
                        .ThenInclude(e => e.Parent)
                    .Include(p => p.Enfant.Classe)
                    .OrderByDescending(p => p.DateEcheance)
                    .ThenByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaiementListeDto
                    {
                        Id = p.Id,
                        TypePaiement = p.TypePaiement,
                        Montant = p.Montant,
                        DatePaiement = p.DatePaiement,
                        DateEcheance = p.DateEcheance,
                        ModePaiement = p.ModePaiement,
                        Statut = p.Statut,
                        NumeroPiece = p.NumeroPiece,
                        Description = p.Description,
                        Trimestre = p.Trimestre,
                        AnneeScolaire = p.AnneeScolaire,
                        EnfantId = p.EnfantId,
                        EnfantNom = $"{p.Enfant.Prenom} {p.Enfant.Nom}",
                        ClasseNom = p.Enfant.Classe != null ? p.Enfant.Classe.Nom : null,
                        ParentNom = p.Enfant.Parent != null ? p.Enfant.Parent.NomComplet : string.Empty,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des paiements");
            }
        }

        // GET: api/ecoles/{ecoleId}/paiements/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Parent")]
        public async Task<ActionResult<PaiementDetailDto>> GetPaiement(int ecoleId, int id)
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
                    return BadRequest("L'identifiant du paiement est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessPaiement(ecoleId, id))
                {
                    return Forbid("Accès non autorisé à ce paiement");
                }

                var paiement = await _context.Paiements
                    .Include(p => p.Enfant)
                        .ThenInclude(e => e.Parent)
                    .Include(p => p.Enfant.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(p => p.Enfant.Ecole)
                    .Where(p => p.Id == id && p.EcoleId == ecoleId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (paiement == null)
                {
                    return NotFound($"Paiement avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                var paiementDto = new PaiementDetailDto
                {
                    Id = paiement.Id,
                    TypePaiement = paiement.TypePaiement,
                    Montant = paiement.Montant,
                    DatePaiement = paiement.DatePaiement,
                    DateEcheance = paiement.DateEcheance,
                    ModePaiement = paiement.ModePaiement,
                    Statut = paiement.Statut,
                    NumeroPiece = paiement.NumeroPiece,
                    Description = paiement.Description,
                    Trimestre = paiement.Trimestre,
                    AnneeScolaire = paiement.AnneeScolaire,
                    EnfantId = paiement.EnfantId,
                    EnfantNom = $"{paiement.Enfant.Prenom} {paiement.Enfant.Nom}",
                    EnfantDateNaissance = paiement.Enfant.DateNaissance,
                    ClasseNom = paiement.Enfant.Classe?.Nom,
                    EnseignantPrincipalNom = paiement.Enfant.Classe?.EnseignantPrincipal?.NomComplet,
                    ParentId = paiement.Enfant.ParentId,
                    ParentNom = paiement.Enfant.Parent?.NomComplet ?? string.Empty,
                    ParentEmail = paiement.Enfant.Parent?.Email,
                    ParentTelephone = paiement.Enfant.Parent?.Telephone,
                    EcoleNom = paiement.Enfant.Ecole?.Nom ?? string.Empty,
                    CreatedAt = paiement.CreatedAt,
                    UpdatedAt = paiement.UpdatedAt
                };

                return Ok(paiementDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du paiement {PaiementId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du paiement");
            }
        }

        // POST: api/ecoles/{ecoleId}/paiements
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<PaiementDetailDto>> CreatePaiement(int ecoleId, [FromBody] CreatePaiementRequest request)
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

                // Vérifier que l'enfant existe et appartient à cette école
                var enfant = await _context.Enfants
                    .Include(e => e.Parent)
                    .Include(e => e.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(e => e.Ecole)
                    .FirstOrDefaultAsync(e => e.Id == request.EnfantId && e.EcoleId == ecoleId && !e.IsDeleted);

                if (enfant == null)
                {
                    return NotFound($"Enfant avec l'ID {request.EnfantId} non trouvé dans l'école {ecoleId}");
                }

                // Validation des dates
                if (request.DateEcheance < request.DatePaiement)
                {
                    return BadRequest("La date d'échéance ne peut pas être antérieure à la date de paiement");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var paiement = new Paiement
                {
                    EnfantId = request.EnfantId,
                    TypePaiement = request.TypePaiement,
                    Montant = request.Montant,
                    DatePaiement = request.DatePaiement,
                    DateEcheance = request.DateEcheance,
                    ModePaiement = request.ModePaiement,
                    Statut = request.Statut,
                    NumeroPiece = request.NumeroPiece,
                    Description = request.Description,
                    Trimestre = request.Trimestre,
                    AnneeScolaire = request.AnneeScolaire,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId
                };

                _context.Paiements.Add(paiement);
                await _context.SaveChangesAsync();

                // Recharger le paiement avec les relations
                paiement = await _context.Paiements
                    .Include(p => p.Enfant)
                        .ThenInclude(e => e.Parent)
                    .Include(p => p.Enfant.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(p => p.Enfant.Ecole)
                    .FirstAsync(p => p.Id == paiement.Id);

                _logger.LogInformation("Paiement de {Montant} FCFA ({TypePaiement}) créé pour l'enfant {EnfantId} avec l'ID {Id}",
                    paiement.Montant, paiement.TypePaiement, request.EnfantId, paiement.Id);

                var result = new PaiementDetailDto
                {
                    Id = paiement.Id,
                    TypePaiement = paiement.TypePaiement,
                    Montant = paiement.Montant,
                    DatePaiement = paiement.DatePaiement,
                    DateEcheance = paiement.DateEcheance,
                    ModePaiement = paiement.ModePaiement,
                    Statut = paiement.Statut,
                    NumeroPiece = paiement.NumeroPiece,
                    Description = paiement.Description,
                    Trimestre = paiement.Trimestre,
                    AnneeScolaire = paiement.AnneeScolaire,
                    EnfantId = paiement.EnfantId,
                    EnfantNom = $"{paiement.Enfant.Prenom} {paiement.Enfant.Nom}",
                    EnfantDateNaissance = paiement.Enfant.DateNaissance,
                    ClasseNom = paiement.Enfant.Classe?.Nom,
                    EnseignantPrincipalNom = paiement.Enfant.Classe?.EnseignantPrincipal?.NomComplet,
                    ParentId = paiement.Enfant.ParentId,
                    ParentNom = paiement.Enfant.Parent?.NomComplet ?? string.Empty,
                    ParentEmail = paiement.Enfant.Parent?.Email,
                    ParentTelephone = paiement.Enfant.Parent?.Telephone,
                    EcoleNom = paiement.Enfant.Ecole?.Nom ?? string.Empty,
                    CreatedAt = paiement.CreatedAt
                };

                return CreatedAtAction(nameof(GetPaiement), new { ecoleId, id = paiement.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du paiement pour l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création du paiement");
            }
        }

        // PUT: api/ecoles/{ecoleId}/paiements/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdatePaiement(int ecoleId, int id, [FromBody] UpdatePaiementRequest request)
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
                    return BadRequest("L'identifiant du paiement est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManagePaiement(ecoleId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de modifier ce paiement");
                }

                var paiement = await _context.Paiements
                    .FirstOrDefaultAsync(p => p.Id == id && p.EcoleId == ecoleId && !p.IsDeleted);

                if (paiement == null)
                {
                    return NotFound($"Paiement avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.TypePaiement))
                    paiement.TypePaiement = request.TypePaiement;
                if (request.Montant.HasValue)
                    paiement.Montant = request.Montant.Value;
                if (request.DatePaiement.HasValue)
                    paiement.DatePaiement = request.DatePaiement.Value;
                if (request.DateEcheance.HasValue)
                    paiement.DateEcheance = request.DateEcheance.Value;
                if (!string.IsNullOrEmpty(request.ModePaiement))
                    paiement.ModePaiement = request.ModePaiement;
                if (!string.IsNullOrEmpty(request.Statut))
                    paiement.Statut = request.Statut;
                if (request.NumeroPiece != null)
                    paiement.NumeroPiece = string.IsNullOrEmpty(request.NumeroPiece) ? null : request.NumeroPiece;
                if (request.Description != null)
                    paiement.Description = string.IsNullOrEmpty(request.Description) ? null : request.Description;
                if (!string.IsNullOrEmpty(request.Trimestre))
                    paiement.Trimestre = request.Trimestre;
                if (!string.IsNullOrEmpty(request.AnneeScolaire))
                    paiement.AnneeScolaire = request.AnneeScolaire;

                // Validation des dates après mise à jour
                if (paiement.DateEcheance < paiement.DatePaiement)
                {
                    return BadRequest("La date d'échéance ne peut pas être antérieure à la date de paiement");
                }

                paiement.UpdatedAt = DateTime.UtcNow;
                paiement.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Paiement {Id} mis à jour dans l'école {EcoleId}", id, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du paiement {PaiementId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du paiement");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/paiements/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeletePaiement(int ecoleId, int id)
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
                    return BadRequest("L'identifiant du paiement est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManagePaiement(ecoleId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de supprimer ce paiement");
                }

                var paiement = await _context.Paiements
                    .FirstOrDefaultAsync(p => p.Id == id && p.EcoleId == ecoleId && !p.IsDeleted);

                if (paiement == null)
                {
                    return NotFound($"Paiement avec l'ID {id} non trouvé dans l'école {ecoleId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                paiement.IsDeleted = true;
                paiement.UpdatedAt = DateTime.UtcNow;
                paiement.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Paiement {TypePaiement} de {Montant} FCFA supprimé (soft delete) de l'école {EcoleId}",
                    paiement.TypePaiement, paiement.Montant, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du paiement {PaiementId} de l'école {EcoleId}", id, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du paiement");
            }
        }

        // GET: api/ecoles/{ecoleId}/paiements/enfant/{enfantId}
        [HttpGet("enfant/{enfantId:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Parent")]
        public async Task<ActionResult<IEnumerable<PaiementListeDto>>> GetPaiementsEnfant(
            int ecoleId,
            int enfantId,
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

                var query = _context.Paiements
                    .Where(p => p.EnfantId == enfantId && p.EcoleId == ecoleId && !p.IsDeleted);

                // Filtrage par année scolaire
                if (!string.IsNullOrEmpty(anneeScolaire))
                {
                    query = query.Where(p => p.AnneeScolaire == anneeScolaire);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var paiements = await query
                    .Include(p => p.Enfant)
                        .ThenInclude(e => e.Parent)
                    .Include(p => p.Enfant.Classe)
                    .OrderByDescending(p => p.DateEcheance)
                    .ThenByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaiementListeDto
                    {
                        Id = p.Id,
                        TypePaiement = p.TypePaiement,
                        Montant = p.Montant,
                        DatePaiement = p.DatePaiement,
                        DateEcheance = p.DateEcheance,
                        ModePaiement = p.ModePaiement,
                        Statut = p.Statut,
                        NumeroPiece = p.NumeroPiece,
                        Description = p.Description,
                        Trimestre = p.Trimestre,
                        AnneeScolaire = p.AnneeScolaire,
                        EnfantId = p.EnfantId,
                        EnfantNom = $"{p.Enfant.Prenom} {p.Enfant.Nom}",
                        ClasseNom = p.Enfant.Classe != null ? p.Enfant.Classe.Nom : null,
                        ParentNom = p.Enfant.Parent != null ? p.Enfant.Parent.NomComplet : string.Empty,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements de l'enfant {EnfantId} de l'école {EcoleId}", enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des paiements");
            }
        }

        // GET: api/ecoles/{ecoleId}/paiements/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<PaiementStatistiquesDto>> GetStatistiques(int ecoleId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var paiements = await _context.Paiements
                    .Where(p => p.EcoleId == ecoleId && !p.IsDeleted)
                    .ToListAsync();

                var ecole = await _context.Ecoles.FindAsync(ecoleId);

                var maintenant = DateTime.Now;
                var debutMois = new DateTime(maintenant.Year, maintenant.Month, 1);
                var debutAnnee = new DateTime(maintenant.Year, 1, 1);

                var statistiques = new PaiementStatistiquesDto
                {
                    EcoleId = ecoleId,
                    EcoleNom = ecole?.Nom ?? string.Empty,
                    NombreTotalPaiements = paiements.Count,
                    MontantTotalPaiements = paiements.Sum(p => p.Montant),
                    NombrePaiementsPayes = paiements.Count(p => p.Statut == "paye"),
                    MontantPaiementsPayes = paiements.Where(p => p.Statut == "paye").Sum(p => p.Montant),
                    NombrePaiementsEnAttente = paiements.Count(p => p.Statut == "en_attente"),
                    MontantPaiementsEnAttente = paiements.Where(p => p.Statut == "en_attente").Sum(p => p.Montant),
                    NombrePaiementsEnRetard = paiements.Count(p => p.Statut == "retard"),
                    MontantPaiementsEnRetard = paiements.Where(p => p.Statut == "retard").Sum(p => p.Montant),
                    PaiementsParType = paiements.GroupBy(p => p.TypePaiement)
                        .Select(g => new PaiementTypeStatDto
                        {
                            TypePaiement = g.Key,
                            NombrePaiements = g.Count(),
                            MontantTotal = g.Sum(p => p.Montant),
                            NombrePayes = g.Count(p => p.Statut == "paye"),
                            MontantPaye = g.Where(p => p.Statut == "paye").Sum(p => p.Montant)
                        })
                        .OrderByDescending(t => t.MontantTotal)
                        .ToList(),
                    PaiementsParMode = paiements.GroupBy(p => p.ModePaiement)
                        .Select(g => new PaiementModeStatDto
                        {
                            ModePaiement = g.Key,
                            NombrePaiements = g.Count(),
                            MontantTotal = g.Sum(p => p.Montant)
                        })
                        .OrderByDescending(m => m.MontantTotal)
                        .ToList()
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des paiements de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/ecoles/{ecoleId}/paiements/export-excel
        [HttpGet("export-excel")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ExportPaiementsExcel(
            int ecoleId,
            [FromQuery] string? typePaiement = null,
            [FromQuery] string? statut = null,
            [FromQuery] string? anneeScolaire = null,
            [FromQuery] DateTime? dateDebutEcheance = null,
            [FromQuery] DateTime? dateFinEcheance = null)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEcole(ecoleId))
                {
                    return Forbid("Accès non autorisé à cette école");
                }

                var query = _context.Paiements
                    .Include(p => p.Enfant)
                        .ThenInclude(e => e.Parent)
                    .Include(p => p.Enfant.Classe)
                    .Where(p => p.EcoleId == ecoleId && !p.IsDeleted);

                // Appliquer les mêmes filtres que pour la liste
                if (!string.IsNullOrEmpty(typePaiement))
                    query = query.Where(p => p.TypePaiement == typePaiement);
                if (!string.IsNullOrEmpty(statut))
                    query = query.Where(p => p.Statut == statut);
                if (!string.IsNullOrEmpty(anneeScolaire))
                    query = query.Where(p => p.AnneeScolaire == anneeScolaire);
                if (dateDebutEcheance.HasValue)
                    query = query.Where(p => p.DateEcheance >= dateDebutEcheance.Value);
                if (dateFinEcheance.HasValue)
                    query = query.Where(p => p.DateEcheance <= dateFinEcheance.Value);

                var paiements = await query
                    .OrderByDescending(p => p.DateEcheance)
                    .ThenByDescending(p => p.CreatedAt)
                    .ToListAsync();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Paiements");

                // En-têtes
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Enfant";
                worksheet.Cells[1, 3].Value = "Classe";
                worksheet.Cells[1, 4].Value = "Parent";
                worksheet.Cells[1, 5].Value = "Type Paiement";
                worksheet.Cells[1, 6].Value = "Montant (FCFA)";
                worksheet.Cells[1, 7].Value = "Date Paiement";
                worksheet.Cells[1, 8].Value = "Date Échéance";
                worksheet.Cells[1, 9].Value = "Mode Paiement";
                worksheet.Cells[1, 10].Value = "Statut";
                worksheet.Cells[1, 11].Value = "N° Pièce";
                worksheet.Cells[1, 12].Value = "Trimestre";
                worksheet.Cells[1, 13].Value = "Année Scolaire";
                worksheet.Cells[1, 14].Value = "Description";
                worksheet.Cells[1, 15].Value = "Date Création";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 15])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                for (int i = 0; i < paiements.Count; i++)
                {
                    var paiement = paiements[i];
                    var row = i + 2;

                    worksheet.Cells[row, 1].Value = paiement.Id;
                    worksheet.Cells[row, 2].Value = $"{paiement.Enfant.Prenom} {paiement.Enfant.Nom}";
                    worksheet.Cells[row, 3].Value = paiement.Enfant.Classe?.Nom ?? "Non assigné";
                    worksheet.Cells[row, 4].Value = paiement.Enfant.Parent?.NomComplet ?? "Non trouvé";
                    worksheet.Cells[row, 5].Value = paiement.TypePaiement;
                    worksheet.Cells[row, 6].Value = paiement.Montant;
                    worksheet.Cells[row, 7].Value = paiement.DatePaiement.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 8].Value = paiement.DateEcheance.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 9].Value = paiement.ModePaiement;
                    worksheet.Cells[row, 10].Value = paiement.Statut;
                    worksheet.Cells[row, 11].Value = paiement.NumeroPiece ?? "";
                    worksheet.Cells[row, 12].Value = paiement.Trimestre;
                    worksheet.Cells[row, 13].Value = paiement.AnneeScolaire;
                    worksheet.Cells[row, 14].Value = paiement.Description ?? "";
                    worksheet.Cells[row, 15].Value = paiement.CreatedAt.ToString("dd/MM/yyyy HH:mm");

                    // Colorer selon le statut
                    var statusCell = worksheet.Cells[row, 10];
                    switch (paiement.Statut)
                    {
                        case "paye":
                            statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                            break;
                        case "retard":
                            statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                            break;
                        case "en_attente":
                            statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                            break;
                    }
                }

                // Auto-fit des colonnes
                worksheet.Cells.AutoFitColumns();

                // Ligne de totaux
                var totalRow = paiements.Count + 3;
                worksheet.Cells[totalRow, 5].Value = "TOTAL:";
                worksheet.Cells[totalRow, 6].Value = paiements.Sum(p => p.Montant);
                worksheet.Cells[totalRow, 5, totalRow, 6].Style.Font.Bold = true;

                var ecole = await _context.Ecoles.FindAsync(ecoleId);
                var fileBytes = package.GetAsByteArray();
                var fileName = $"Paiements_{ecole?.Code}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Excel des paiements de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de l'export Excel");
            }
        }

        // GET: api/ecoles/{ecoleId}/paiements/types
        [HttpGet("types")]
        [Authorize(Roles = "SuperAdmin,Admin,Parent")]
        public IActionResult GetTypesPaiement()
        {
            var types = new[]
            {
                new { Value = "scolarite", Label = "Scolarité" },
                new { Value = "cantine", Label = "Cantine" },
                new { Value = "transport", Label = "Transport" },
                new { Value = "activite", Label = "Activité" },
                new { Value = "inscription", Label = "Inscription" },
                new { Value = "materiel", Label = "Matériel scolaire" },
                new { Value = "autre", Label = "Autre" }
            };

            return Ok(types);
        }

        // GET: api/ecoles/{ecoleId}/paiements/modes
        [HttpGet("modes")]
        [Authorize(Roles = "SuperAdmin,Admin,Parent")]
        public IActionResult GetModesPaiement()
        {
            var modes = new[]
            {
                new { Value = "especes", Label = "Espèces" },
                new { Value = "cheque", Label = "Chèque" },
                new { Value = "virement", Label = "Virement bancaire" },
                new { Value = "mobile_money", Label = "Mobile Money" },
                new { Value = "carte", Label = "Carte bancaire" }
            };

            return Ok(modes);
        }

        // GET: api/ecoles/{ecoleId}/paiements/statuts
        [HttpGet("statuts")]
        [Authorize(Roles = "SuperAdmin,Admin,Parent")]
        public IActionResult GetStatutsPaiement()
        {
            var statuts = new[]
            {
                new { Value = "en_attente", Label = "En attente" },
                new { Value = "paye", Label = "Payé" },
                new { Value = "retard", Label = "En retard" },
                new { Value = "annule", Label = "Annulé" }
            };

            return Ok(statuts);
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

        private async Task<bool> CanAccessPaiement(int ecoleId, int paiementId)
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
            {
                var paiement = await _context.Paiements
                    .Include(p => p.Enfant)
                    .FirstOrDefaultAsync(p => p.Id == paiementId);

                return paiement?.Enfant.ParentId == userId;
            }

            return false;
        }

        private async Task<bool> CanManagePaiement(int ecoleId, int paiementId)
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

            // Seuls SuperAdmin et Admin peuvent gérer les paiements
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

            if (User.IsInRole("Admin"))
                return true;

            if (User.IsInRole("Parent"))
            {
                var enfant = await _context.Enfants
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.ParentId == userId);
                return enfant != null;
            }

            return false;
        }
    }

    // DTOs pour les paiements
    public class PaiementListeDto
    {
        public int Id { get; set; }
        public string TypePaiement { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DatePaiement { get; set; }
        public DateTime DateEcheance { get; set; }
        public string ModePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string? NumeroPiece { get; set; }
        public string? Description { get; set; }
        public string Trimestre { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public int EnfantId { get; set; }
        public string EnfantNom { get; set; } = string.Empty;
        public string? ClasseNom { get; set; }
        public string ParentNom { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PaiementDetailDto : PaiementListeDto
    {
        public DateTime EnfantDateNaissance { get; set; }
        public string? EnseignantPrincipalNom { get; set; }
        public string ParentId { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public string? ParentTelephone { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreatePaiementRequest
    {
        [Required(ErrorMessage = "L'enfant est obligatoire")]
        public int EnfantId { get; set; }

        [Required(ErrorMessage = "Le type de paiement est obligatoire")]
        public string TypePaiement { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }

        [Required(ErrorMessage = "La date de paiement est obligatoire")]
        public DateTime DatePaiement { get; set; }

        [Required(ErrorMessage = "La date d'échéance est obligatoire")]
        public DateTime DateEcheance { get; set; }

        [Required(ErrorMessage = "Le mode de paiement est obligatoire")]
        public string ModePaiement { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le statut est obligatoire")]
        public string Statut { get; set; } = "en_attente";

        public string? NumeroPiece { get; set; }
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le trimestre est obligatoire")]
        public string Trimestre { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'année scolaire est obligatoire")]
        public string AnneeScolaire { get; set; } = string.Empty;
    }

    public class UpdatePaiementRequest
    {
        public string? TypePaiement { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal? Montant { get; set; }

        public DateTime? DatePaiement { get; set; }
        public DateTime? DateEcheance { get; set; }
        public string? ModePaiement { get; set; }
        public string? Statut { get; set; }
        public string? NumeroPiece { get; set; }
        public string? Description { get; set; }
        public string? Trimestre { get; set; }
        public string? AnneeScolaire { get; set; }
    }

    public class PaiementStatistiquesDto
    {
        public int EcoleId { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public int NombreTotalPaiements { get; set; }
        public decimal MontantTotalPaiements { get; set; }
        public int NombrePaiementsPayes { get; set; }
        public decimal MontantPaiementsPayes { get; set; }
        public int NombrePaiementsEnAttente { get; set; }
        public decimal MontantPaiementsEnAttente { get; set; }
        public int NombrePaiementsEnRetard { get; set; }
        public decimal MontantPaiementsEnRetard { get; set; }
        public List<PaiementTypeStatDto> PaiementsParType { get; set; } = new List<PaiementTypeStatDto>();
        public List<PaiementModeStatDto> PaiementsParMode { get; set; } = new List<PaiementModeStatDto>();
    }

    public class PaiementTypeStatDto
    {
        public string TypePaiement { get; set; } = string.Empty;
        public int NombrePaiements { get; set; }
        public decimal MontantTotal { get; set; }
        public int NombrePayes { get; set; }
        public decimal MontantPaye { get; set; }
    }

    public class PaiementModeStatDto
    {
        public string ModePaiement { get; set; } = string.Empty;
        public int NombrePaiements { get; set; }
        public decimal MontantTotal { get; set; }
    }
}