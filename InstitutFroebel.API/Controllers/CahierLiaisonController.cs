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
    [Route("api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison")]
    [ApiController]
    [Authorize]
    public class CahierLiaisonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CahierLiaisonController> _logger;

        public CahierLiaisonController(
            ApplicationDbContext context,
            ILogger<CahierLiaisonController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<IEnumerable<CahierLiaisonListeDto>>> GetMessages(
            int ecoleId,
            int enfantId,
            [FromQuery] string? type = null,
            [FromQuery] bool? luParParent = null,
            [FromQuery] bool? reponseRequise = null,
            [FromQuery] string? createdById = null,
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

                var query = _context.CahierLiaisons
                    .Where(c => c.EnfantId == enfantId && !c.IsDeleted);

                // Filtrage par type
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(c => c.Type == type);
                }

                // Filtrage par statut de lecture
                if (luParParent.HasValue)
                {
                    query = query.Where(c => c.LuParParent == luParParent.Value);
                }

                // Filtrage par réponse requise
                if (reponseRequise.HasValue)
                {
                    query = query.Where(c => c.ReponseRequise == reponseRequise.Value);
                }

                // Filtrage par créateur
                if (!string.IsNullOrEmpty(createdById))
                {
                    query = query.Where(c => c.CreatedById == createdById);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var cahierLiaisons = await query
                    .Include(c => c.Enfant)
                        .ThenInclude(e => e.ParentsEnfants)
                            .ThenInclude(pe => pe.Parent)
                    .Include(c => c.Enfant.Classe)
                    .Include(c => c.CreatedBy)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var messages = cahierLiaisons.Select(c => new CahierLiaisonListeDto
                {
                    Id = c.Id,
                    Titre = c.Titre,
                    Message = c.Message.Length > 100 ? c.Message.Substring(0, 100) + "..." : c.Message,
                    Type = c.Type,
                    LuParParent = c.LuParParent,
                    DateLecture = c.DateLecture,
                    ReponseRequise = c.ReponseRequise,
                    ReponseParent = c.ReponseParent,
                    DateReponse = c.DateReponse,
                    EnfantId = c.EnfantId,
                    EnfantNom = $"{c.Enfant.Prenom} {c.Enfant.Nom}",
                    ClasseNom = c.Enfant.Classe != null ? c.Enfant.Classe.Nom : null,
                    ParentNom = c.Enfant.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty,
                    CreatedById = c.CreatedById,
                    CreatedByNom = c.CreatedBy != null ? c.CreatedBy.NomComplet : "Système",
                    CreatedAt = c.CreatedAt
                }).ToList();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des messages du cahier de liaison de l'enfant {EnfantId} de l'école {EcoleId}", enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des messages");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<CahierLiaisonDetailDto>> GetMessage(int ecoleId, int enfantId, int id)
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
                    return BadRequest("L'identifiant du message est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessEnfant(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé à cet enfant");
                }

                var message = await _context.CahierLiaisons
                    .Include(c => c.Enfant)
                        .ThenInclude(e => e.ParentsEnfants)
                            .ThenInclude(pe => pe.Parent)
                    .Include(c => c.Enfant.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(c => c.Enfant.Ecole)
                    .Include(c => c.CreatedBy)
                    .Where(c => c.Id == id && c.EnfantId == enfantId && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                if (message == null)
                {
                    return NotFound($"Message avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                // Marquer comme lu si c'est un parent qui consulte
                if (User.IsInRole("Parent") && !message.LuParParent)
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (message.Enfant.ParentsEnfants.Any(pe => pe.ParentId == currentUserId))
                    {
                        message.LuParParent = true;
                        message.DateLecture = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                // Désérialiser les fichiers si présents
                List<string> fichiers = new List<string>();
                if (!string.IsNullOrEmpty(message.Fichiers))
                {
                    try
                    {
                        fichiers = JsonSerializer.Deserialize<List<string>>(message.Fichiers) ?? new List<string>();
                    }
                    catch (JsonException)
                    {
                        // Si la désérialisation échoue, on garde une liste vide
                        fichiers = new List<string>();
                    }
                }

                var messageDto = new CahierLiaisonDetailDto
                {
                    Id = message.Id,
                    Titre = message.Titre,
                    Message = message.Message,
                    Type = message.Type,
                    Fichiers = fichiers,
                    LuParParent = message.LuParParent,
                    DateLecture = message.DateLecture,
                    ReponseRequise = message.ReponseRequise,
                    ReponseParent = message.ReponseParent,
                    DateReponse = message.DateReponse,
                    EnfantId = message.EnfantId,
                    EnfantNom = $"{message.Enfant.Prenom} {message.Enfant.Nom}",
                    EnfantDateNaissance = message.Enfant.DateNaissance,
                    ClasseNom = message.Enfant.Classe?.Nom,
                    EnseignantPrincipalNom = message.Enfant.Classe?.EnseignantPrincipal?.NomComplet,
                    ParentId = message.Enfant.ParentsEnfants.FirstOrDefault()?.ParentId ?? string.Empty,
                    ParentNom = message.Enfant.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty,
                    ParentEmail = message.Enfant.ParentsEnfants.FirstOrDefault()?.Parent?.Email,
                    EcoleNom = message.Enfant.Ecole?.Nom ?? string.Empty,
                    CreatedById = message.CreatedById,
                    CreatedByNom = message.CreatedBy?.NomComplet ?? "Système",
                    CreatedAt = message.CreatedAt,
                    UpdatedAt = message.UpdatedAt
                };

                return Ok(messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du message {MessageId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du message");
            }
        }

        // POST: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult<CahierLiaisonDetailDto>> CreateMessage(int ecoleId, int enfantId, [FromBody] CreateCahierLiaisonRequest request)
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
                    return Forbid("Vous n'avez pas l'autorisation de créer des messages pour cet enfant");
                }

                // Vérifier que l'enfant existe
                var enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Include(e => e.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(e => e.Ecole)
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.EcoleId == ecoleId && !e.IsDeleted);

                if (enfant == null)
                {
                    return NotFound($"Enfant avec l'ID {enfantId} non trouvé dans l'école {ecoleId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Sérialiser les fichiers si présents
                string? fichiersJson = null;
                if (request.Fichiers != null && request.Fichiers.Any())
                {
                    fichiersJson = JsonSerializer.Serialize(request.Fichiers);
                }

                var message = new CahierLiaison
                {
                    EnfantId = enfantId,
                    Titre = request.Titre,
                    Message = request.Message,
                    Type = request.Type,
                    Fichiers = fichiersJson,
                    ReponseRequise = request.ReponseRequise,
                    LuParParent = false,
                    EcoleId = ecoleId,
                    CreatedById = currentUserId ?? string.Empty
                };

                _context.CahierLiaisons.Add(message);
                await _context.SaveChangesAsync();

                // Recharger le message avec les relations
                message = await _context.CahierLiaisons
                    .Include(c => c.Enfant)
                        .ThenInclude(e => e.ParentsEnfants)
                            .ThenInclude(pe => pe.Parent)
                    .Include(c => c.Enfant.Classe)
                        .ThenInclude(c => c!.EnseignantPrincipal)
                    .Include(c => c.Enfant.Ecole)
                    .Include(c => c.CreatedBy)
                    .FirstAsync(c => c.Id == message.Id);

                _logger.LogInformation("Message '{Titre}' créé pour l'enfant {EnfantId} de l'école {EcoleId} avec l'ID {Id}",
                    message.Titre, enfantId, ecoleId, message.Id);

                var result = new CahierLiaisonDetailDto
                {
                    Id = message.Id,
                    Titre = message.Titre,
                    Message = message.Message,
                    Type = message.Type,
                    Fichiers = request.Fichiers ?? new List<string>(),
                    LuParParent = message.LuParParent,
                    DateLecture = message.DateLecture,
                    ReponseRequise = message.ReponseRequise,
                    ReponseParent = message.ReponseParent,
                    DateReponse = message.DateReponse,
                    EnfantId = message.EnfantId,
                    EnfantNom = $"{message.Enfant.Prenom} {message.Enfant.Nom}",
                    EnfantDateNaissance = message.Enfant.DateNaissance,
                    ClasseNom = message.Enfant.Classe?.Nom,
                    EnseignantPrincipalNom = message.Enfant.Classe?.EnseignantPrincipal?.NomComplet,
                    ParentId = message.Enfant.ParentsEnfants.FirstOrDefault()?.ParentId ?? string.Empty,
                    ParentNom = message.Enfant.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty,
                    ParentEmail = message.Enfant.ParentsEnfants.FirstOrDefault()?.Parent?.Email,
                    EcoleNom = message.Enfant.Ecole?.Nom ?? string.Empty,
                    CreatedById = message.CreatedById,
                    CreatedByNom = message.CreatedBy?.NomComplet ?? "Système",
                    CreatedAt = message.CreatedAt
                };

                return CreatedAtAction(nameof(GetMessage), new { ecoleId, enfantId, id = message.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du message pour l'enfant {EnfantId} de l'école {EcoleId}", enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la création du message");
            }
        }

        // PUT: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<IActionResult> UpdateMessage(int ecoleId, int enfantId, int id, [FromBody] UpdateCahierLiaisonRequest request)
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
                    return BadRequest("L'identifiant du message est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageMessage(ecoleId, enfantId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de modifier ce message");
                }

                var message = await _context.CahierLiaisons
                    .FirstOrDefaultAsync(c => c.Id == id && c.EnfantId == enfantId && !c.IsDeleted);

                if (message == null)
                {
                    return NotFound($"Message avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Titre))
                    message.Titre = request.Titre;
                if (!string.IsNullOrEmpty(request.Message))
                    message.Message = request.Message;
                if (!string.IsNullOrEmpty(request.Type))
                    message.Type = request.Type;
                if (request.ReponseRequise.HasValue)
                    message.ReponseRequise = request.ReponseRequise.Value;

                // Mettre à jour les fichiers si fournis
                if (request.Fichiers != null)
                {
                    message.Fichiers = request.Fichiers.Any() ? JsonSerializer.Serialize(request.Fichiers) : null;
                }

                message.UpdatedAt = DateTime.UtcNow;
                message.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Message {Id} mis à jour pour l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du message {MessageId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du message");
            }
        }

        // DELETE: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<IActionResult> DeleteMessage(int ecoleId, int enfantId, int id)
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
                    return BadRequest("L'identifiant du message est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageMessage(ecoleId, enfantId, id))
                {
                    return Forbid("Vous n'avez pas l'autorisation de supprimer ce message");
                }

                var message = await _context.CahierLiaisons
                    .FirstOrDefaultAsync(c => c.Id == id && c.EnfantId == enfantId && !c.IsDeleted);

                if (message == null)
                {
                    return NotFound($"Message avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Soft delete
                message.IsDeleted = true;
                message.UpdatedAt = DateTime.UtcNow;
                message.UpdatedById = currentUserId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Message '{Titre}' supprimé (soft delete) pour l'enfant {EnfantId} de l'école {EcoleId}",
                    message.Titre, enfantId, ecoleId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du message {MessageId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du message");
            }
        }

        // POST: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison/{id}/repondre
        [HttpPost("{id:int}/repondre")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> RepondreMessage(int ecoleId, int enfantId, int id, [FromBody] RepondreCahierLiaisonRequest request)
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
                    return BadRequest("L'identifiant du message est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que c'est bien le parent de l'enfant
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.EcoleId == ecoleId && !e.IsDeleted && e.ParentsEnfants.Any(pe => pe.ParentId == currentUserId));

                if (enfant == null)
                {
                    return Forbid("Vous ne pouvez répondre qu'aux messages de vos propres enfants");
                }

                var message = await _context.CahierLiaisons
                    .FirstOrDefaultAsync(c => c.Id == id && c.EnfantId == enfantId && !c.IsDeleted);

                if (message == null)
                {
                    return NotFound($"Message avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                if (!message.ReponseRequise)
                {
                    return BadRequest("Ce message ne nécessite pas de réponse");
                }

                // Ajouter la réponse
                message.ReponseParent = request.Reponse;
                message.DateReponse = DateTime.UtcNow;
                message.LuParParent = true;
                message.DateLecture = message.DateLecture ?? DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Réponse ajoutée au message {MessageId} pour l'enfant {EnfantId} par le parent {ParentId}",
                    id, enfantId, currentUserId);

                return Ok(new { Message = "Réponse enregistrée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réponse au message {MessageId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors de l'enregistrement de la réponse");
            }
        }

        // PUT: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison/{id}/marquer-lu
        [HttpPut("{id:int}/marquer-lu")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> MarquerMessageLu(int ecoleId, int enfantId, int id)
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
                    return BadRequest("L'identifiant du message est invalide");
                }

                // Vérifier que c'est bien le parent de l'enfant
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.EcoleId == ecoleId && !e.IsDeleted && e.ParentsEnfants.Any(pe => pe.ParentId == currentUserId));

                if (enfant == null)
                {
                    return Forbid("Vous ne pouvez marquer comme lu que les messages de vos propres enfants");
                }

                var message = await _context.CahierLiaisons
                    .FirstOrDefaultAsync(c => c.Id == id && c.EnfantId == enfantId && !c.IsDeleted);

                if (message == null)
                {
                    return NotFound($"Message avec l'ID {id} non trouvé pour l'enfant {enfantId}");
                }

                if (message.LuParParent)
                {
                    return Ok(new { Message = "Message déjà marqué comme lu" });
                }

                // Marquer comme lu
                message.LuParParent = true;
                message.DateLecture = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Message {MessageId} marqué comme lu par le parent {ParentId} pour l'enfant {EnfantId}",
                    id, currentUserId, enfantId);

                return Ok(new { Message = "Message marqué comme lu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage comme lu du message {MessageId} de l'enfant {EnfantId} de l'école {EcoleId}", id, enfantId, ecoleId);
                return StatusCode(500, "Une erreur est survenue lors du marquage comme lu");
            }
        }

        // GET: api/ecoles/{ecoleId}/enfants/{enfantId}/cahier-liaison/statistiques
        [HttpGet("statistiques")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher,Parent")]
        public async Task<ActionResult<CahierLiaisonStatistiquesDto>> GetStatistiques(int ecoleId, int enfantId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessEnfant(ecoleId, enfantId))
                {
                    return Forbid("Accès non autorisé à cet enfant");
                }

                var messages = await _context.CahierLiaisons
                    .Where(c => c.EnfantId == enfantId && !c.IsDeleted)
                    .ToListAsync();

                var enfant = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Include(e => e.Classe)
                    .FirstOrDefaultAsync(e => e.Id == enfantId);

                var statistiques = new CahierLiaisonStatistiquesDto
                {
                    EnfantId = enfantId,
                    EnfantNom = enfant != null ? $"{enfant.Prenom} {enfant.Nom}" : string.Empty,
                    ClasseNom = enfant?.Classe?.Nom,
                    ParentNom = enfant?.ParentsEnfants.FirstOrDefault()?.Parent?.NomComplet ?? string.Empty,
                    NombreTotalMessages = messages.Count,
                    NombreMessagesLus = messages.Count(m => m.LuParParent),
                    NombreMessagesNonLus = messages.Count(m => !m.LuParParent),
                    NombreReponsesRequises = messages.Count(m => m.ReponseRequise),
                    NombreReponsesRecues = messages.Count(m => !string.IsNullOrEmpty(m.ReponseParent)),
                    MessagesParType = messages.GroupBy(m => m.Type)
                        .Select(g => new MessageTypeStatDto
                        {
                            Type = g.Key,
                            Nombre = g.Count(),
                            NombreLus = g.Count(m => m.LuParParent),
                            NombreAvecReponse = g.Count(m => !string.IsNullOrEmpty(m.ReponseParent))
                        })
                        .OrderByDescending(t => t.Nombre)
                        .ToList()
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques du cahier de liaison de l'enfant {EnfantId} de l'école {EcoleId}", enfantId, ecoleId);
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
                    .Include(e => e.ParentsEnfants)
                    .FirstOrDefaultAsync(e => e.Id == enfantId && e.ParentsEnfants.Any(pe => pe.ParentId == userId));
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

        private async Task<bool> CanManageMessage(int ecoleId, int enfantId, int messageId)
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
                // Vérifier si l'enseignant est le créateur du message ou l'enseignant principal de la classe
                var message = await _context.CahierLiaisons
                    .Include(c => c.Enfant)
                        .ThenInclude(e => e.Classe)
                    .FirstOrDefaultAsync(c => c.Id == messageId && c.EnfantId == enfantId);

                return message != null &&
                       (message.CreatedById == userId || message.Enfant.Classe?.EnseignantPrincipalId == userId);
            }

            return false;
        }
    }

    // DTOs pour le cahier de liaison
    public class CahierLiaisonListeDto
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty; // Tronqué à 100 caractères
        public string Type { get; set; } = string.Empty;
        public bool LuParParent { get; set; }
        public DateTime? DateLecture { get; set; }
        public bool ReponseRequise { get; set; }
        public string? ReponseParent { get; set; }
        public DateTime? DateReponse { get; set; }
        public int EnfantId { get; set; }
        public string EnfantNom { get; set; } = string.Empty;
        public string? ClasseNom { get; set; }
        public string ParentNom { get; set; } = string.Empty;
        public string CreatedById { get; set; } = string.Empty;
        public string CreatedByNom { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CahierLiaisonDetailDto : CahierLiaisonListeDto
    {
        public new string Message { get; set; } = string.Empty; // Message complet
        public List<string> Fichiers { get; set; } = new List<string>();
        public DateTime EnfantDateNaissance { get; set; }
        public string? EnseignantPrincipalNom { get; set; }
        public string ParentId { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public string EcoleNom { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCahierLiaisonRequest
    {
        [Required(ErrorMessage = "Le titre est obligatoire")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le message est obligatoire")]
        public string Message { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le type est obligatoire")]
        public string Type { get; set; } = "info";

        public List<string>? Fichiers { get; set; }
        public bool ReponseRequise { get; set; } = false;
    }

    public class UpdateCahierLiaisonRequest
    {
        public string? Titre { get; set; }
        public string? Message { get; set; }
        public string? Type { get; set; }
        public List<string>? Fichiers { get; set; }
        public bool? ReponseRequise { get; set; }
    }

    public class RepondreCahierLiaisonRequest
    {
        [Required(ErrorMessage = "La réponse est obligatoire")]
        public string Reponse { get; set; } = string.Empty;
    }

    public class CahierLiaisonStatistiquesDto
    {
        public int EnfantId { get; set; }
        public string EnfantNom { get; set; } = string.Empty;
        public string? ClasseNom { get; set; }
        public string ParentNom { get; set; } = string.Empty;
        public int NombreTotalMessages { get; set; }
        public int NombreMessagesLus { get; set; }
        public int NombreMessagesNonLus { get; set; }
        public int NombreReponsesRequises { get; set; }
        public int NombreReponsesRecues { get; set; }
        public List<MessageTypeStatDto> MessagesParType { get; set; } = new List<MessageTypeStatDto>();
    }

    public class MessageTypeStatDto
    {
        public string Type { get; set; } = string.Empty;
        public int Nombre { get; set; }
        public int NombreLus { get; set; }
        public int NombreAvecReponse { get; set; }
    }
}