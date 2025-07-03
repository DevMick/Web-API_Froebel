using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InstitutFroebel.API.Data;
using InstitutFroebel.API.DTOs.Auth;
using InstitutFroebel.API.DTOs.Common;
using InstitutFroebel.API.DTOs.User;
using InstitutFroebel.API.DTOs.School;
using InstitutFroebel.API.Services.Interfaces;
using InstitutFroebel.Core.Entities.Identity;
using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.Core.Interfaces;

namespace InstitutFroebel.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            IMapper mapper,
            ApplicationDbContext context,
            ITenantService tenantService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _mapper = mapper;
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, string role)
        {
            try
            {
                // Vérifier si l'école existe
                var school = await _context.Ecoles
                    .FirstOrDefaultAsync(s => s.Id == registerDto.SchoolId);

                if (school == null)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("École non trouvée ou inactive");
                }

                // Vérifier si l'email existe déjà dans cette école
                var existingUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email && u.EcoleId == school.Id);

                if (existingUser != null)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("Un utilisateur avec cet email existe déjà dans cette école");
                }

                // Créer l'utilisateur
                var user = _mapper.Map<ApplicationUser>(registerDto);
                user.EcoleId = school.Id;
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.EmailConfirmed = true; // Confirmer automatiquement l'email

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<AuthResponseDto>.ErrorResult("Erreur lors de la création du compte", errors);
                }

                // Assigner le rôle
                await _userManager.AddToRoleAsync(user, role);

                // Charger l'utilisateur avec l'école
                user = await _userManager.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .FirstAsync(u => u.Id == user.Id);

                // Générer le token
                var token = await _jwtService.GenerateTokenAsync(user);
                var refreshToken = await _jwtService.GenerateRefreshTokenAsync();

                // Mapper les données utilisateur
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user) as List<string> ?? new List<string>();

                var schoolDto = _mapper.Map<SchoolDto>(school);

                var authResponse = new AuthResponseDto
                {
                    Success = true,
                    Message = "Compte créé avec succès",
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiration = DateTime.UtcNow.AddMinutes(60),
                    User = userDto,
                    School = schoolDto
                };

                _logger.LogInformation("Nouvel utilisateur enregistré: {Email} dans l'école {SchoolId} avec le rôle {Role}", registerDto.Email, registerDto.SchoolId, role);

                return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Inscription réussie");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'inscription pour {Email}", registerDto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResult("Erreur interne du serveur");
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
        {
            try
            {
                // Vérifier si l'école existe
                var school = await _context.Ecoles
                    .FirstOrDefaultAsync(s => s.Id == loginDto.SchoolId);

                if (school == null)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("École non trouvée ou inactive");
                }

                // Chercher l'utilisateur dans cette école
                var user = await _userManager.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.EcoleId == school.Id);

                if (user == null)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("Email ou mot de passe incorrect");
                }

                // SUPPRESSION DE LA VÉRIFICATION EmailConfirmed
                // La vérification EmailConfirmed a été supprimée pour permettre la connexion sans confirmation d'email

                // Vérifier si le compte est verrouillé/désactivé
                if (user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("Compte désactivé");
                }

                // Vérifier le mot de passe
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, true);

                if (!result.Succeeded)
                {
                    if (result.IsLockedOut)
                    {
                        return ApiResponse<AuthResponseDto>.ErrorResult("Compte verrouillé temporairement");
                    }
                    return ApiResponse<AuthResponseDto>.ErrorResult("Email ou mot de passe incorrect");
                }

                // Générer les tokens
                var token = await _jwtService.GenerateTokenAsync(user);
                var refreshToken = await _jwtService.GenerateRefreshTokenAsync();

                // Mapper les données
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user) as List<string> ?? new List<string>();

                var schoolDto = _mapper.Map<SchoolDto>(school);

                var authResponse = new AuthResponseDto
                {
                    Success = true,
                    Message = "Connexion réussie",
                    Token = token,
                    RefreshToken = refreshToken,
                    TokenExpiration = DateTime.UtcNow.AddMinutes(60),
                    User = userDto,
                    School = schoolDto
                };

                _logger.LogInformation("Connexion réussie pour {Email} dans l'école {SchoolId}", loginDto.Email, loginDto.SchoolId);

                return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Connexion réussie");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion pour {Email}", loginDto.Email);
                return ApiResponse<AuthResponseDto>.ErrorResult("Erreur interne du serveur");
            }
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var principal = _jwtService.GetPrincipalFromExpiredToken(refreshTokenDto.Token);
                if (principal == null)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("Token invalide");
                }

                var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("Token invalide");
                }

                var user = await _userManager.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                // SUPPRESSION DE LA VÉRIFICATION EmailConfirmed dans RefreshToken également
                if (user == null)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("Utilisateur non trouvé");
                }

                // Vérifier si le compte est verrouillé/désactivé
                if (user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow)
                {
                    return ApiResponse<AuthResponseDto>.ErrorResult("Compte désactivé");
                }

                // Générer un nouveau token
                var newToken = await _jwtService.GenerateTokenAsync(user);
                var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync();

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user) as List<string> ?? new List<string>();

                var schoolDto = _mapper.Map<SchoolDto>(user.Ecole);

                var authResponse = new AuthResponseDto
                {
                    Success = true,
                    Message = "Token renouvelé",
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    TokenExpiration = DateTime.UtcNow.AddMinutes(60),
                    User = userDto,
                    School = schoolDto
                };

                return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Token renouvelé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du renouvellement du token");
                return ApiResponse<AuthResponseDto>.ErrorResult("Erreur lors du renouvellement du token");
            }
        }

        public async Task<ApiResponse> LogoutAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    // Invalider le security stamp pour forcer la déconnexion
                    await _userManager.UpdateSecurityStampAsync(user);
                    _logger.LogInformation("Déconnexion pour l'utilisateur {UserId}", userId);
                }

                return ApiResponse.SuccessResult("Déconnexion réussie");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion pour {UserId}", userId);
                return ApiResponse.ErrorResult("Erreur lors de la déconnexion");
            }
        }

        public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return ApiResponse<UserDto>.ErrorResult("Utilisateur non trouvé");
                }

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user) as List<string> ?? new List<string>();

                return ApiResponse<UserDto>.SuccessResult(userDto, "Profil utilisateur récupéré");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du profil pour {UserId}", userId);
                return ApiResponse<UserDto>.ErrorResult("Erreur lors de la récupération du profil");
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateProfileAsync(string userId, UpdateUserDto updateDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<UserDto>.ErrorResult("Utilisateur non trouvé");
                }

                // Mettre à jour les propriétés
                _mapper.Map(updateDto, user);
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<UserDto>.ErrorResult("Erreur lors de la mise à jour", errors);
                }

                // Recharger l'utilisateur avec les relations
                user = await _userManager.Users
                    .Include(u => u.Ecole)
                    .Include(u => u.EnfantsAsParent)
                        .ThenInclude(pe => pe.Enfant)
                    .FirstAsync(u => u.Id == userId);

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = await _userManager.GetRolesAsync(user) as List<string> ?? new List<string>();

                return ApiResponse<UserDto>.SuccessResult(userDto, "Profil mis à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du profil pour {UserId}", userId);
                return ApiResponse<UserDto>.ErrorResult("Erreur lors de la mise à jour du profil");
            }
        }

        public async Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse.ErrorResult("Utilisateur non trouvé");
                }

                var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse.ErrorResult("Erreur lors du changement de mot de passe", errors);
                }

                _logger.LogInformation("Mot de passe changé pour l'utilisateur {UserId}", userId);
                return ApiResponse.SuccessResult("Mot de passe changé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de mot de passe pour {UserId}", userId);
                return ApiResponse.ErrorResult("Erreur lors du changement de mot de passe");
            }
        }

        public async Task<ApiResponse> DeleteAccountAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse.ErrorResult("Utilisateur non trouvé");
                }

                // Supprimer le compte
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return ApiResponse.ErrorResult("Erreur lors de la suppression du compte");
                }

                _logger.LogInformation("Compte supprimé pour l'utilisateur {UserId}", userId);
                return ApiResponse.SuccessResult("Compte supprimé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du compte pour {UserId}", userId);
                return ApiResponse.ErrorResult("Erreur lors de la suppression du compte");
            }
        }

        public async Task<ApiResponse<List<string>>> GetUserRolesAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<List<string>>.ErrorResult("Utilisateur non trouvé");
                }

                var roles = await _userManager.GetRolesAsync(user);
                return ApiResponse<List<string>>.SuccessResult(roles.ToList(), "Rôles récupérés avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rôles pour {UserId}", userId);
                return ApiResponse<List<string>>.ErrorResult("Erreur lors de la récupération des rôles");
            }
        }

        public async Task<List<SchoolDto>> GetAvailableSchoolsAsync()
        {
            try
            {
                var schools = await _context.Ecoles
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.Nom)
                    .Select(s => new SchoolDto
                    {
                        Id = s.Id,
                        Nom = s.Nom,
                        Code = s.Code,
                        Adresse = s.Adresse,
                        Commune = s.Commune,
                        Telephone = s.Telephone,
                        Email = s.Email,
                        AnneeScolaire = s.AnneeScolaire
                    })
                    .ToListAsync();

                return schools;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des écoles disponibles");
                return new List<SchoolDto>();
            }
        }
    }
}