using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.Identity;
using InstitutFroebel.Core.Entities.School;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/ecoles/{ecoleId}/preinscriptions")]
    [ApiController]
    public class PreinscriptionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PreinscriptionController> _logger;

        public PreinscriptionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<PreinscriptionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // POST: api/ecoles/{ecoleId}/preinscriptions
        [HttpPost]
        public async Task<ActionResult<PreinscriptionResultDto>> CreatePreinscription(
            int ecoleId,
            [FromBody] CreatePreinscriptionRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

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

                // Vérifier que l'école existe
                var ecole = await _context.Ecoles
                    .FirstOrDefaultAsync(e => e.Id == ecoleId && !e.IsDeleted);

                if (ecole == null)
                {
                    return NotFound("École non trouvée");
                }

                // Vérifier l'unicité de l'email du parent
                var existingUser = await _userManager.FindByEmailAsync(request.Parent.Email);
                if (existingUser != null)
                {
                    return BadRequest($"Un utilisateur avec l'email '{request.Parent.Email}' existe déjà");
                }

                // Valider les classes pour chaque enfant
                foreach (var enfantRequest in request.Enfants)
                {
                    if (enfantRequest.ClasseId.HasValue)
                    {
                        var classe = await _context.Classes
                            .FirstOrDefaultAsync(c => c.Id == enfantRequest.ClasseId.Value &&
                                                    c.EcoleId == ecoleId &&
                                                    !c.IsDeleted);
                        if (classe == null)
                        {
                            return BadRequest($"Classe avec l'ID {enfantRequest.ClasseId.Value} non trouvée dans cette école");
                        }
                    }
                }

                // 1. Créer l'utilisateur parent
                var parent = new ApplicationUser
                {
                    UserName = request.Parent.Email,
                    Email = request.Parent.Email,
                    Nom = request.Parent.Nom,
                    Prenom = request.Parent.Prenom,
                    Telephone = request.Parent.Telephone,
                    Adresse = request.Parent.Adresse,
                    Sexe = request.Parent.Sexe,
                    DateNaissance = request.Parent.DateNaissance,
                    EcoleId = ecoleId,
                    EmailConfirmed = true, // Email confirmé automatiquement
                    CreatedAt = DateTime.UtcNow
                };

                var createUserResult = await _userManager.CreateAsync(parent, request.Parent.MotDePasse);
                if (!createUserResult.Succeeded)
                {
                    var errors = createUserResult.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new
                    {
                        Message = "Erreur lors de la création du parent",
                        Errors = errors
                    });
                }

                // Assigner le rôle Parent
                await _userManager.AddToRoleAsync(parent, "Parent");

                _logger.LogInformation("Parent créé : {Email} pour l'école {EcoleId}", parent.Email, ecoleId);

                // 2. Créer les enfants
                var enfantsCreated = new List<Enfant>();

                foreach (var enfantRequest in request.Enfants)
                {
                    var enfant = new Enfant
                    {
                        Nom = enfantRequest.Nom,
                        Prenom = enfantRequest.Prenom,
                        DateNaissance = enfantRequest.DateNaissance,
                        Sexe = enfantRequest.Sexe,
                        ClasseId = enfantRequest.ClasseId,
                        AnneeScolaire = ecole.AnneeScolaire, // Utiliser l'année scolaire de l'école
                        Statut = "pre_inscrit", // Statut par défaut pour préinscription
                        DateInscription = DateTime.UtcNow,
                        UtiliseCantine = enfantRequest.UtiliseCantine,
                        EcoleId = ecoleId,
                        CreatedById = parent.Id
                    };

                    _context.Enfants.Add(enfant);
                    enfantsCreated.Add(enfant);
                }

                await _context.SaveChangesAsync();

                // 3. Créer les relations parent-enfant
                foreach (var enfant in enfantsCreated)
                {
                    var parentEnfant = new ParentEnfant
                    {
                        ParentId = parent.Id,
                        EnfantId = enfant.Id,
                        EcoleId = ecoleId,
                        CreatedById = parent.Id
                    };

                    _context.ParentEnfants.Add(parentEnfant);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Préinscription complétée : Parent {ParentId} avec {NombreEnfants} enfant(s) dans l'école {EcoleId}",
                    parent.Id, enfantsCreated.Count, ecoleId);

                // 4. Préparer la réponse
                var result = new PreinscriptionResultDto
                {
                    ParentId = parent.Id,
                    ParentEmail = parent.Email,
                    ParentNom = parent.NomComplet,
                    EcoleId = ecoleId,
                    EcoleNom = ecole.Nom,
                    Enfants = enfantsCreated.Select(e => new EnfantPreinscriptionDto
                    {
                        Id = e.Id,
                        Nom = e.Nom,
                        Prenom = e.Prenom,
                        DateNaissance = e.DateNaissance,
                        Sexe = e.Sexe,
                        ClasseId = e.ClasseId,
                        AnneeScolaire = e.AnneeScolaire,
                        UtiliseCantine = e.UtiliseCantine,
                        Statut = e.Statut
                    }).ToList(),
                    DatePreinscription = DateTime.UtcNow,
                    Message = "Préinscription effectuée avec succès. Vous pouvez maintenant vous connecter avec vos identifiants."
                };

                // TODO: Envoyer un email de bienvenue au parent
                // await _emailService.SendWelcomeEmailAsync(parent);

                return CreatedAtAction(nameof(GetPreinscription),
                    new { ecoleId, preinscriptionId = parent.Id },
                    result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la préinscription pour l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la préinscription");
            }
        }

        // GET: api/ecoles/{ecoleId}/preinscriptions/{preinscriptionId}
        [HttpGet("{preinscriptionId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<PreinscriptionDetailDto>> GetPreinscription(int ecoleId, string preinscriptionId)
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

                var parent = await _context.Users
                    .Include(u => u.EnfantsAsParent.Where(pe => !pe.IsDeleted))
                        .ThenInclude(pe => pe.Enfant)
                            .ThenInclude(e => e.Classe)
                    .Include(u => u.Ecole)
                    .FirstOrDefaultAsync(u => u.Id == preinscriptionId && u.EcoleId == ecoleId);

                if (parent == null)
                {
                    return NotFound("Préinscription non trouvée");
                }

                var roles = await _userManager.GetRolesAsync(parent);
                if (!roles.Contains("Parent"))
                {
                    return BadRequest("L'utilisateur spécifié n'est pas un parent");
                }

                var result = new PreinscriptionDetailDto
                {
                    ParentId = parent.Id,
                    ParentEmail = parent.Email ?? string.Empty,
                    ParentNom = parent.NomComplet,
                    ParentTelephone = parent.Telephone,
                    ParentAdresse = parent.Adresse,
                    EstValide = parent.EnfantsAsParent.Any(pe => !pe.IsDeleted && pe.Enfant.Statut == "inscrit"),
                    EcoleId = ecoleId,
                    EcoleNom = parent.Ecole?.Nom ?? string.Empty,
                    Enfants = parent.EnfantsAsParent
                        .Where(pe => !pe.IsDeleted && !pe.Enfant.IsDeleted)
                        .Select(pe => new EnfantPreinscriptionDetailDto
                        {
                            Id = pe.Enfant.Id,
                            Nom = pe.Enfant.Nom,
                            Prenom = pe.Enfant.Prenom,
                            DateNaissance = pe.Enfant.DateNaissance,
                            Sexe = pe.Enfant.Sexe,
                            ClasseId = pe.Enfant.ClasseId,
                            ClasseNom = pe.Enfant.Classe?.Nom,
                            AnneeScolaire = pe.Enfant.AnneeScolaire,
                            UtiliseCantine = pe.Enfant.UtiliseCantine,
                            Statut = pe.Enfant.Statut,
                            DateInscription = pe.Enfant.DateInscription
                        }).ToList(),
                    DatePreinscription = parent.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la préinscription {PreinscriptionId} de l'école {EcoleId}",
                    preinscriptionId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la préinscription");
            }
        }

        // GET: api/ecoles/{ecoleId}/preinscriptions
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<IEnumerable<PreinscriptionListeDto>>> GetPreinscriptions(
            int ecoleId,
            [FromQuery] bool? estValide = null,
            [FromQuery] string? statutEnfant = null,
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

                // Obtenir tous les parents de cette école
                var parentsInRole = await _userManager.GetUsersInRoleAsync("Parent");
                var parentIds = parentsInRole.Where(u => u.EcoleId == ecoleId).Select(u => u.Id).ToList();

                var query = _context.Users.Where(u => parentIds.Contains(u.Id));

                // Filtrage par statut de validation
                if (estValide.HasValue)
                {
                    if (estValide.Value)
                    {
                        // Parents avec au moins un enfant inscrit
                        query = query.Where(u => u.EnfantsAsParent.Any(pe => !pe.IsDeleted && pe.Enfant.Statut == "inscrit"));
                    }
                    else
                    {
                        // Parents avec tous les enfants en pré-inscription
                        query = query.Where(u => u.EnfantsAsParent.All(pe => !pe.IsDeleted && pe.Enfant.Statut == "pre_inscrit"));
                    }
                }

                // Filtrage par statut des enfants
                if (!string.IsNullOrEmpty(statutEnfant))
                {
                    query = query.Where(u => u.EnfantsAsParent.Any(pe => !pe.IsDeleted && pe.Enfant.Statut == statutEnfant));
                }

                // Filtrage par période
                if (dateDebut.HasValue)
                {
                    query = query.Where(u => u.CreatedAt >= dateDebut.Value);
                }
                if (dateFin.HasValue)
                {
                    query = query.Where(u => u.CreatedAt <= dateFin.Value);
                }

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(u => u.Nom.ToLower().Contains(termeLower) ||
                                           u.Prenom.ToLower().Contains(termeLower) ||
                                           u.Email!.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var preinscriptions = await query
                    .Include(u => u.EnfantsAsParent.Where(pe => !pe.IsDeleted))
                        .ThenInclude(pe => pe.Enfant)
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new PreinscriptionListeDto
                    {
                        ParentId = u.Id,
                        ParentEmail = u.Email ?? string.Empty,
                        ParentNom = u.NomComplet,
                        ParentTelephone = u.Telephone,
                        EstValide = u.EnfantsAsParent.Any(pe => !pe.IsDeleted && pe.Enfant.Statut == "inscrit"),
                        NombreEnfants = u.EnfantsAsParent.Count(pe => !pe.IsDeleted),
                        NombreEnfantsPreInscrits = u.EnfantsAsParent.Count(pe => !pe.IsDeleted && pe.Enfant.Statut == "pre_inscrit"),
                        NombreEnfantsInscrits = u.EnfantsAsParent.Count(pe => !pe.IsDeleted && pe.Enfant.Statut == "inscrit"),
                        DatePreinscription = u.CreatedAt
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(preinscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des préinscriptions de l'école {EcoleId}", ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des préinscriptions");
            }
        }

        // PUT: api/ecoles/{ecoleId}/preinscriptions/{preinscriptionId}/valider
        [HttpPut("{preinscriptionId}/valider")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ValiderPreinscription(int ecoleId, string preinscriptionId)
        {
            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var parent = await _context.Users
                    .Include(u => u.EnfantsAsParent.Where(pe => !pe.IsDeleted))
                        .ThenInclude(pe => pe.Enfant)
                    .FirstOrDefaultAsync(u => u.Id == preinscriptionId && u.EcoleId == ecoleId);

                if (parent == null)
                {
                    return NotFound("Préinscription non trouvée");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour le statut des enfants à "inscrit"
                foreach (var parentEnfant in parent.EnfantsAsParent.Where(pe => !pe.IsDeleted))
                {
                    if (parentEnfant.Enfant.Statut == "pre_inscrit")
                    {
                        parentEnfant.Enfant.Statut = "inscrit";
                        parentEnfant.Enfant.UpdatedAt = DateTime.UtcNow;
                        parentEnfant.Enfant.UpdatedById = currentUserId;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Préinscription validée pour le parent {ParentId} dans l'école {EcoleId}",
                    preinscriptionId, ecoleId);

                return Ok(new { Message = "Préinscription validée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de la préinscription {PreinscriptionId} de l'école {EcoleId}",
                    preinscriptionId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la validation de la préinscription");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/preinscriptions/{preinscriptionId}
        [HttpDelete("{preinscriptionId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeletePreinscription(int ecoleId, string preinscriptionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validation des paramètres
                if (ecoleId <= 0)
                {
                    return BadRequest("L'identifiant de l'école est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageEcole(ecoleId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer cette école");
                }

                var parent = await _context.Users
                    .Include(u => u.EnfantsAsParent.Where(pe => !pe.IsDeleted))
                        .ThenInclude(pe => pe.Enfant)
                    .FirstOrDefaultAsync(u => u.Id == preinscriptionId && u.EcoleId == ecoleId);

                if (parent == null)
                {
                    return NotFound("Préinscription non trouvée");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete des enfants et relations
                foreach (var parentEnfant in parent.EnfantsAsParent.Where(pe => !pe.IsDeleted))
                {
                    // Soft delete de l'enfant
                    parentEnfant.Enfant.IsDeleted = true;
                    parentEnfant.Enfant.UpdatedAt = DateTime.UtcNow;
                    parentEnfant.Enfant.UpdatedById = currentUserId;

                    // Soft delete de la relation parent-enfant
                    parentEnfant.IsDeleted = true;
                    parentEnfant.UpdatedAt = DateTime.UtcNow;
                    parentEnfant.UpdatedById = currentUserId;
                }

                // Supprimer le parent (hard delete car c'est un utilisateur)
                var deleteResult = await _userManager.DeleteAsync(parent);
                if (!deleteResult.Succeeded)
                {
                    var errors = deleteResult.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { Message = "Erreur lors de la suppression", Errors = errors });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Préinscription supprimée pour le parent {ParentId} dans l'école {EcoleId}",
                    preinscriptionId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la suppression de la préinscription {PreinscriptionId} de l'école {EcoleId}",
                    preinscriptionId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la préinscription");
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

    // DTOs pour la préinscription
    public class CreatePreinscriptionRequest
    {
        [Required(ErrorMessage = "Les informations du parent sont obligatoires")]
        public CreateParentRequest Parent { get; set; } = null!;

        [Required(ErrorMessage = "Au moins un enfant est obligatoire")]
        [MinLength(1, ErrorMessage = "Au moins un enfant est obligatoire")]
        public List<CreateEnfantPreinscriptionRequest> Enfants { get; set; } = new List<CreateEnfantPreinscriptionRequest>();
    }

    public class CreateParentRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string Email { get; set; } = string.Empty;

        public string? Telephone { get; set; }
        public string? Adresse { get; set; }

        [Required(ErrorMessage = "Le sexe est obligatoire")]
        public string Sexe { get; set; } = string.Empty;

        public DateTime? DateNaissance { get; set; }

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string MotDePasse { get; set; } = string.Empty;
    }

    public class CreateEnfantPreinscriptionRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date de naissance est obligatoire")]
        public DateTime DateNaissance { get; set; }

        [Required(ErrorMessage = "Le sexe est obligatoire")]
        public string Sexe { get; set; } = string.Empty;

        public int? ClasseId { get; set; }
        public bool UtiliseCantine { get; set; } = false;
    }

    public class PreinscriptionResultDto
    {
        public string ParentId { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string ParentNom { get; set; } = string.Empty;
        public int EcoleId { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public List<EnfantPreinscriptionDto> Enfants { get; set; } = new List<EnfantPreinscriptionDto>();
        public DateTime DatePreinscription { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EnfantPreinscriptionDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public DateTime DateNaissance { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public int? ClasseId { get; set; }
        public string AnneeScolaire { get; set; } = string.Empty;
        public bool UtiliseCantine { get; set; }
        public string Statut { get; set; } = string.Empty;
    }

    public class PreinscriptionDetailDto
    {
        public string ParentId { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string ParentNom { get; set; } = string.Empty;
        public string? ParentTelephone { get; set; }
        public string? ParentAdresse { get; set; }
        public bool EstValide { get; set; }
        public int EcoleId { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public List<EnfantPreinscriptionDetailDto> Enfants { get; set; } = new List<EnfantPreinscriptionDetailDto>();
        public DateTime DatePreinscription { get; set; }
    }

    public class EnfantPreinscriptionDetailDto : EnfantPreinscriptionDto
    {
        public string? ClasseNom { get; set; }
        public DateTime? DateInscription { get; set; }
    }

    public class PreinscriptionListeDto
    {
        public string ParentId { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public string ParentNom { get; set; } = string.Empty;
        public string? ParentTelephone { get; set; }
        public bool EstValide { get; set; }
        public int NombreEnfants { get; set; }
        public int NombreEnfantsPreInscrits { get; set; }
        public int NombreEnfantsInscrits { get; set; }
        public DateTime DatePreinscription { get; set; }
    }
}