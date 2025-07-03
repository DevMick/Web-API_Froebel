using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.DTOs.Auth;
using InstitutFroebel.API.Services.Interfaces;
using InstitutFroebel.API.DTOs.Common;
using InstitutFroebel.API.DTOs.User;
using InstitutFroebel.API.DTOs.School;
using InstitutFroebel.API.Data;
using InstitutFroebel.Core.Entities.Identity;

namespace InstitutFroebel.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(loginDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion pour {Email}", loginDto.Email);
                return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResult("Erreur interne du serveur"));
            }
        }

        // POST: api/auth/register/parent
        [HttpPost("register/parent")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterParent([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(registerDto, "Parent");
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'inscription d'un parent pour {Email}", registerDto.Email);
                return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResult("Erreur interne du serveur"));
            }
        }

        // POST: api/auth/register/admin
        [HttpPost("register/admin")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterAdmin([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(registerDto, "Admin");
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'inscription d'un admin pour {Email}", registerDto.Email);
                return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResult("Erreur interne du serveur"));
            }
        }

        // POST: api/auth/register/teacher
        [HttpPost("register/teacher")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterTeacher([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(registerDto, "Teacher");
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'inscription d'un enseignant pour {Email}", registerDto.Email);
                return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResult("Erreur interne du serveur"));
            }
        }

        // POST: api/auth/register/initial-admin (pour le premier SuperAdmin)
        [HttpPost("register/initial-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterInitialAdmin([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier s'il existe déjà un SuperAdmin
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            if (superAdmins.Any())
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Un SuperAdmin existe déjà. Utilisez l'endpoint /register/admin."
                });
            }

            var result = await _authService.RegisterAsync(registerDto, "SuperAdmin");
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RefreshTokenAsync(refreshTokenDto);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du renouvellement du token");
                return StatusCode(500, ApiResponse<AuthResponseDto>.ErrorResult("Erreur interne du serveur"));
            }
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(ApiResponse.ErrorResult("Utilisateur non identifié"));
                }

                var result = await _authService.LogoutAsync(userId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion");
                return StatusCode(500, ApiResponse.ErrorResult("Erreur interne du serveur"));
            }
        }

        // GET: api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(ApiResponse<UserDto>.ErrorResult("Utilisateur non identifié"));
                }

                var result = await _authService.GetCurrentUserAsync(userId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du profil utilisateur");
                return StatusCode(500, ApiResponse<UserDto>.ErrorResult("Erreur interne du serveur"));
            }
        }

        // GET: api/auth/schools
        [HttpGet("schools")]
        public async Task<ActionResult<ApiResponse<List<SchoolDto>>>> GetSchools()
        {
            try
            {
                var schools = await _authService.GetAvailableSchoolsAsync();
                return Ok(ApiResponse<List<SchoolDto>>.SuccessResult(schools, "Écoles récupérées avec succès"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des écoles");
                return StatusCode(500, ApiResponse<List<SchoolDto>>.ErrorResult("Erreur interne du serveur"));
            }
        }

        // NOUVEAUX ENDPOINTS AVEC ACCÈS DIRECT AUX DONNÉES

        // GET: api/auth/school/{schoolId}/users
        [HttpGet("school/{schoolId}/users")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult> GetSchoolUsers(int schoolId)
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .Include(u => u.EnfantsAsTeacher)
                        .ThenInclude(te => te.Enfant)
                    .Where(u => u.EcoleId == schoolId)
                    .OrderBy(u => u.Nom)
                    .ThenBy(u => u.Prenom)
                    .ToListAsync();

                var result = new List<object>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    var userData = new
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Nom = user.Nom,
                        Prenom = user.Prenom,
                        NomComplet = user.NomComplet,
                        Telephone = user.Telephone,
                        Adresse = user.Adresse,
                        EcoleId = user.EcoleId,
                        EcoleNom = user.Ecole?.Nom,
                        EmailConfirmed = user.EmailConfirmed,
                        IsActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd < DateTime.UtcNow,
                        CreatedAt = user.CreatedAt,
                        CreatedAtFormatted = user.CreatedAt.ToString("dd/MM/yyyy"),
                        Roles = roles.ToArray(),
                        EnfantsCount = (user.EnfantsAsParent?.Count ?? 0) + (user.EnfantsAsTeacher?.Count ?? 0)
                    };

                    result.Add(userData);
                }

                return Ok(new
                {
                    Success = true,
                    SchoolId = schoolId,
                    SchoolName = users.FirstOrDefault()?.Ecole?.Nom,
                    UserCount = result.Count,
                    Users = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs de l'école {SchoolId}", schoolId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des utilisateurs",
                    Error = ex.Message
                });
            }
        }

        // GET: api/auth/school/{schoolId}/user/{userId}
        [HttpGet("school/{schoolId}/user/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult> GetSchoolUser(int schoolId, string userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .Include(u => u.EnfantsAsTeacher)
                        .ThenInclude(te => te.Enfant)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.EcoleId == schoolId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé dans cette école"
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var enfantsData = user.EnfantsAsParent?.Select(pe => new
                {
                    Id = pe.Enfant.Id,
                    Nom = pe.Enfant.Nom,
                    Prenom = pe.Enfant.Prenom,
                    DateNaissance = pe.Enfant.DateNaissance,
                    DateNaissanceFormatted = pe.Enfant.DateNaissance.ToString("dd/MM/yyyy"),
                    Relation = "Parent"
                }).Concat(user.EnfantsAsTeacher?.Select(te => new
                {
                    Id = te.Enfant.Id,
                    Nom = te.Enfant.Nom,
                    Prenom = te.Enfant.Prenom,
                    DateNaissance = te.Enfant.DateNaissance,
                    DateNaissanceFormatted = te.Enfant.DateNaissance.ToString("dd/MM/yyyy"),
                    Relation = "Teacher"
                }) ?? Enumerable.Empty<dynamic>()) ?? Enumerable.Empty<dynamic>();

                var userData = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    NomComplet = user.NomComplet,
                    Telephone = user.Telephone,
                    Adresse = user.Adresse,
                    EcoleId = user.EcoleId,
                    EcoleNom = user.Ecole?.Nom,
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd < DateTime.UtcNow,
                    CreatedAt = user.CreatedAt,
                    CreatedAtFormatted = user.CreatedAt.ToString("dd/MM/yyyy"),
                    Roles = roles.ToArray(),
                    Enfants = enfantsData,
                    EnfantsCount = (user.EnfantsAsParent?.Count ?? 0) + (user.EnfantsAsTeacher?.Count ?? 0)
                };

                return Ok(new
                {
                    Success = true,
                    User = userData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur {UserId} de l'école {SchoolId}", userId, schoolId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération de l'utilisateur",
                    Error = ex.Message
                });
            }
        }

        // GET: api/auth/user/{userId}/schools
        [HttpGet("user/{userId}/schools")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> GetUserSchools(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                var userWithSchool = await _context.Users
                    .Include(u => u.Ecole)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                var schoolInfo = userWithSchool?.Ecole != null ? new
                {
                    Id = userWithSchool.Ecole.Id,
                    Nom = userWithSchool.Ecole.Nom,
                    Adresse = userWithSchool.Ecole.Adresse,
                    Telephone = userWithSchool.Ecole.Telephone
                } : null;

                return Ok(new
                {
                    Success = true,
                    UserId = userId,
                    UserInfo = new
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Nom = user.Nom,
                        Prenom = user.Prenom,
                        NomComplet = user.NomComplet
                    },
                    School = schoolInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'école de l'utilisateur {UserId}", userId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération de l'école de l'utilisateur",
                    Error = ex.Message
                });
            }
        }

        // GET: api/auth/all-users-with-schools
        [HttpGet("all-users-with-schools")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> GetAllUsersWithSchools()
        {
            try
            {
                var usersWithSchools = await _context.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                    .Include(u => u.EnfantsAsTeacher)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Nom = u.Nom,
                        Prenom = u.Prenom,
                        NomComplet = u.NomComplet,
                        Telephone = u.Telephone,
                        Adresse = u.Adresse,
                        UserCreatedDate = u.CreatedAt,
                        UserCreatedDateFormatted = u.CreatedAt.ToString("dd/MM/yyyy"),
                        IsActive = !u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd < DateTime.UtcNow,
                        EnfantsCount = (u.EnfantsAsParent != null ? u.EnfantsAsParent.Count : 0) + (u.EnfantsAsTeacher != null ? u.EnfantsAsTeacher.Count : 0),
                        School = u.Ecole != null ? new
                        {
                            Id = u.Ecole.Id,
                            Nom = u.Ecole.Nom,
                            Adresse = u.Ecole.Adresse,
                            Telephone = u.Ecole.Telephone
                        } : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    UserCount = usersWithSchools.Count,
                    Users = usersWithSchools
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les utilisateurs avec leurs écoles");
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération de tous les utilisateurs",
                    Error = ex.Message
                });
            }
        }

        // GET: api/auth/school/{schoolId}/stats
        [HttpGet("school/{schoolId}/stats")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult> GetSchoolStats(int schoolId)
        {
            try
            {
                var school = await _context.Ecoles.FindAsync(schoolId);
                if (school == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "École non trouvée"
                    });
                }

                var users = await _context.Users
                    .Include(u => u.EnfantsAsParent)
                    .Include(u => u.EnfantsAsTeacher)
                    .Where(u => u.EcoleId == schoolId)
                    .ToListAsync();

                var allRoles = await _roleManager.Roles.ToListAsync();
                var roleStats = new Dictionary<string, int>();

                foreach (var role in allRoles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                    roleStats[role.Name] = usersInRole.Count(u => users.Any(su => su.Id == u.Id));
                }

                var activeUsers = users.Count(u => !u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd < DateTime.UtcNow);
                var recentUsers = users.Count(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30));
                var totalEnfants = users.Sum(u => (u.EnfantsAsParent != null ? u.EnfantsAsParent.Count : 0) + (u.EnfantsAsTeacher != null ? u.EnfantsAsTeacher.Count : 0));

                var oldestUser = users.OrderBy(u => u.CreatedAt).FirstOrDefault();
                var newestUser = users.OrderByDescending(u => u.CreatedAt).FirstOrDefault();

                return Ok(new
                {
                    Success = true,
                    SchoolId = schoolId,
                    SchoolName = school.Nom,
                    Stats = new
                    {
                        TotalUsers = users.Count,
                        ActiveUsers = activeUsers,
                        InactiveUsers = users.Count - activeUsers,
                        RecentUsers30Days = recentUsers,
                        TotalEnfants = totalEnfants,
                        RoleDistribution = roleStats,
                        ParentCount = roleStats.GetValueOrDefault("Parent", 0),
                        TeacherCount = roleStats.GetValueOrDefault("Teacher", 0),
                        AdminCount = roleStats.GetValueOrDefault("Admin", 0),
                        SuperAdminCount = roleStats.GetValueOrDefault("SuperAdmin", 0),
                        OldestUser = oldestUser != null ? new
                        {
                            Id = oldestUser.Id,
                            Name = oldestUser.NomComplet,
                            CreatedAt = oldestUser.CreatedAt,
                            CreatedAtFormatted = oldestUser.CreatedAt.ToString("dd/MM/yyyy")
                        } : null,
                        NewestUser = newestUser != null ? new
                        {
                            Id = newestUser.Id,
                            Name = newestUser.NomComplet,
                            CreatedAt = newestUser.CreatedAt,
                            CreatedAtFormatted = newestUser.CreatedAt.ToString("dd/MM/yyyy")
                        } : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de l'école {SchoolId}", schoolId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des statistiques",
                    Error = ex.Message
                });
            }
        }

        // PATCH: api/auth/user/{userId}/deactivate
        [HttpPatch("user/{userId}/deactivate")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult> DeactivateUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                if (user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "L'utilisateur est déjà désactivé"
                    });
                }

                // Empêcher la désactivation du dernier SuperAdmin
                var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
                if (isSuperAdmin)
                {
                    var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                    var activeSuperAdmins = superAdmins.Where(u => !u.LockoutEnabled || u.LockoutEnd == null || u.LockoutEnd < DateTime.UtcNow).ToList();

                    if (activeSuperAdmins.Count <= 1)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = "Impossible de désactiver le dernier SuperAdmin actif"
                        });
                    }
                }

                // Désactiver l'utilisateur
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Erreur lors de la désactivation",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"L'utilisateur {user.NomComplet} a été désactivé",
                    DeactivatedUser = new
                    {
                        UserId = userId,
                        UserName = user.NomComplet,
                        UserEmail = user.Email,
                        DeactivatedDate = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la désactivation de l'utilisateur {UserId}", userId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la désactivation",
                    Error = ex.Message
                });
            }
        }

        // PATCH: api/auth/user/{userId}/activate
        [HttpPatch("user/{userId}/activate")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult> ActivateUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                if (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd < DateTime.UtcNow)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "L'utilisateur est déjà actif"
                    });
                }

                // Réactiver l'utilisateur
                user.LockoutEnabled = false;
                user.LockoutEnd = null;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Erreur lors de l'activation",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"L'utilisateur {user.NomComplet} a été réactivé",
                    ActivatedUser = new
                    {
                        UserId = userId,
                        UserName = user.NomComplet,
                        UserEmail = user.Email,
                        ActivatedDate = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'activation de l'utilisateur {UserId}", userId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de l'activation",
                    Error = ex.Message
                });
            }
        }

        // POST: api/auth/promote-to-admin/{userId}
        [HttpPost("promote-to-admin/{userId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> PromoteToAdmin(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = $"L'utilisateur {user.Email} est déjà Admin"
                    });
                }

                var result = await _userManager.AddToRoleAsync(user, "Admin");

                if (result.Succeeded)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = $"L'utilisateur {user.Email} a été promu Admin avec succès",
                        UserId = userId,
                        Email = user.Email
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Erreur lors de la promotion",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la promotion de l'utilisateur {UserId} en Admin", userId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la promotion",
                    Error = ex.Message
                });
            }
        }

        // DELETE: api/auth/user/{userId}
        [HttpDelete("user/{userId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                // Empêcher la suppression du dernier SuperAdmin
                var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
                if (isSuperAdmin)
                {
                    var superAdminCount = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                    if (superAdminCount.Count <= 1)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = "Impossible de supprimer le dernier Super Administrateur"
                        });
                    }
                }

                // Compter les enfants associés avant suppression
                var enfantsCount = await _context.ParentEnfants.CountAsync(pe => pe.ParentId == userId);

                // Supprimer l'utilisateur (les enfants seront gérés par cascade delete si configuré)
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Erreur lors de la suppression de l'utilisateur",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"L'utilisateur {user.NomComplet} a été supprimé définitivement du système",
                    DeletedUser = new
                    {
                        UserId = userId,
                        UserName = user.NomComplet,
                        UserEmail = user.Email,
                        DeletedDate = DateTime.UtcNow,
                        EnfantsAffected = enfantsCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'utilisateur {UserId}", userId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la suppression de l'utilisateur",
                    Error = ex.Message
                });
            }
        }

        // GET: api/auth/my-user-info
        [HttpGet("my-user-info")]
        [Authorize]
        public async Task<ActionResult> GetMyUserInfo()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        Success = false,
                        Message = "Utilisateur non connecté"
                    });
                }

                var user = await _authService.GetCurrentUserAsync(userId);
                if (!user.Success)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                var isSuperAdmin = User.IsInRole("SuperAdmin");
                var isAdmin = User.IsInRole("Admin");
                var isTeacher = User.IsInRole("Teacher");
                var isParent = User.IsInRole("Parent");

                return Ok(new
                {
                    Success = true,
                    UserId = userId,
                    User = user.Data,
                    IsSuperAdmin = isSuperAdmin,
                    IsAdmin = isAdmin,
                    IsTeacher = isTeacher,
                    IsParent = isParent,
                    Roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                              .Select(c => c.Value).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des informations utilisateur");
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des informations",
                    Error = ex.Message
                });
            }
        }

        // GET: api/auth/school/{schoolId}/children
        [HttpGet("school/{schoolId}/children")]
        [Authorize(Roles = "SuperAdmin,Admin,Teacher")]
        public async Task<ActionResult> GetSchoolChildren(int schoolId)
        {
            try
            {
                var school = await _context.Ecoles.FindAsync(schoolId);
                if (school == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "École non trouvée"
                    });
                }

                var children = await _context.Enfants
                    .Include(e => e.ParentsEnfants)
                        .ThenInclude(pe => pe.Parent)
                    .Where(e => e.EcoleId == schoolId)
                    .OrderBy(e => e.Nom)
                    .ThenBy(e => e.Prenom)
                    .Select(e => new
                    {
                        Id = e.Id,
                        Nom = e.Nom,
                        Prenom = e.Prenom,
                        NomComplet = $"{e.Prenom} {e.Nom}",
                        DateNaissance = e.DateNaissance,
                        DateNaissanceFormatted = e.DateNaissance.ToString("dd/MM/yyyy"),
                        Age = DateTime.Now.Year - e.DateNaissance.Year - (DateTime.Now.DayOfYear < e.DateNaissance.DayOfYear ? 1 : 0),
                        Parents = e.ParentsEnfants.Select(pe => new
                        {
                            Id = pe.Parent.Id,
                            Nom = pe.Parent.Nom,
                            Prenom = pe.Parent.Prenom,
                            NomComplet = pe.Parent.NomComplet,
                            Email = pe.Parent.Email,
                            Telephone = pe.Parent.Telephone
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    SchoolId = schoolId,
                    SchoolName = school.Nom,
                    ChildrenCount = children.Count,
                    Children = children
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des enfants de l'école {SchoolId}", schoolId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des enfants",
                    Error = ex.Message
                });
            }
        }

        // GET: api/auth/user/{userId}/children
        [HttpGet("user/{userId}/children")]
        [Authorize]
        public async Task<ActionResult> GetUserChildren(string userId)
        {
            try
            {
                // Vérifier l'autorisation : l'utilisateur peut voir ses propres enfants ou un admin peut voir tous les enfants
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Admin") || User.IsInRole("Teacher");

                if (currentUserId != userId && !isAdmin)
                {
                    return Forbid("Vous n'êtes pas autorisé à voir les enfants de cet utilisateur");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                var children = await _context.ParentEnfants
                    .Include(pe => pe.Enfant)
                    .Where(pe => pe.ParentId == userId)
                    .OrderBy(pe => pe.Enfant.DateNaissance)
                    .Select(pe => new
                    {
                        Id = pe.Enfant.Id,
                        Nom = pe.Enfant.Nom,
                        Prenom = pe.Enfant.Prenom,
                        NomComplet = $"{pe.Enfant.Prenom} {pe.Enfant.Nom}",
                        DateNaissance = pe.Enfant.DateNaissance,
                        DateNaissanceFormatted = pe.Enfant.DateNaissance.ToString("dd/MM/yyyy"),
                        Age = DateTime.Now.Year - pe.Enfant.DateNaissance.Year - (DateTime.Now.DayOfYear < pe.Enfant.DateNaissance.DayOfYear ? 1 : 0)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    UserId = userId,
                    UserInfo = new
                    {
                        Id = user.Id,
                        Nom = user.Nom,
                        Prenom = user.Prenom,
                        NomComplet = user.NomComplet,
                        Email = user.Email
                    },
                    ChildrenCount = children.Count,
                    Children = children
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des enfants de l'utilisateur {UserId}", userId);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des enfants",
                    Error = ex.Message
                });
            }
        }
    }
}