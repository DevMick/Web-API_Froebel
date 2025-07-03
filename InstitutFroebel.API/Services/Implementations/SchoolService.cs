using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using InstitutFroebel.API.Data;
using InstitutFroebel.API.DTOs.Common;
using InstitutFroebel.API.DTOs.School;
using InstitutFroebel.API.Services.Interfaces;
using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.Core.Entities.Identity;

namespace InstitutFroebel.API.Services.Implementations
{
    public class SchoolService : ISchoolService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SchoolService> _logger;

        public SchoolService(
            ApplicationDbContext context, 
            IMapper mapper, 
            ILogger<SchoolService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<PagedResult<SchoolDto>>> GetAllSchoolsAsync(PagedRequest request)
        {
            try
            {
                var query = _context.Ecoles.AsQueryable();

                // Filtrage par recherche
                if (!string.IsNullOrEmpty(request.Search))
                {
                    query = query.Where(s =>
                        s.Nom.Contains(request.Search) ||
                        s.Code.Contains(request.Search));
                }

                // Tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "nom":
                            query = request.SortDescending ? query.OrderByDescending(s => s.Nom) : query.OrderBy(s => s.Nom);
                            break;
                        case "code":
                            query = request.SortDescending ? query.OrderByDescending(s => s.Code) : query.OrderBy(s => s.Code);
                            break;
                        case "Commune":
                            query = request.SortDescending ? query.OrderByDescending(s => s.Commune) : query.OrderBy(s => s.Commune);
                            break;
                        default:
                            query = query.OrderBy(s => s.CreatedAt);
                            break;
                    }
                }
                else
                {
                    query = query.OrderBy(s => s.Nom);
                }

                var totalCount = await query.CountAsync();

                var schools = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var schoolDtos = _mapper.Map<List<SchoolDto>>(schools);

                var result = new PagedResult<SchoolDto>
                {
                    Items = schoolDtos,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

                return ApiResponse<PagedResult<SchoolDto>>.SuccessResult(result, "Écoles récupérées avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des écoles");
                return ApiResponse<PagedResult<SchoolDto>>.ErrorResult("Erreur lors de la récupération des écoles");
            }
        }

        public async Task<ApiResponse<SchoolDto>> GetSchoolByIdAsync(int id)
        {
            try
            {
                var school = await _context.Ecoles.FindAsync(id);
                if (school == null || school.IsDeleted)
                {
                    return ApiResponse<SchoolDto>.ErrorResult("École non trouvée");
                }

                var schoolDto = _mapper.Map<SchoolDto>(school);
                return ApiResponse<SchoolDto>.SuccessResult(schoolDto, "École récupérée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'école {SchoolId}", id);
                return ApiResponse<SchoolDto>.ErrorResult("Erreur lors de la récupération de l'école");
            }
        }

        public async Task<ApiResponse<SchoolDto>> GetSchoolByCodeAsync(string code)
        {
            try
            {
                var school = await _context.Ecoles
                    .FirstOrDefaultAsync(s => s.Code == code && !s.IsDeleted);

                if (school == null)
                {
                    return ApiResponse<SchoolDto>.ErrorResult("École non trouvée");
                }

                var schoolDto = _mapper.Map<SchoolDto>(school);
                return ApiResponse<SchoolDto>.SuccessResult(schoolDto, "École récupérée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'école avec le code {SchoolCode}", code);
                return ApiResponse<SchoolDto>.ErrorResult("Erreur lors de la récupération de l'école");
            }
        }

        public async Task<ApiResponse<SchoolDto>> CreateSchoolAsync(CreateSchoolDto createDto)
        {
            try
            {
                // Vérifier si le code existe déjà
                var existingSchool = await _context.Ecoles
                    .FirstOrDefaultAsync(s => s.Code == createDto.Code);

                if (existingSchool != null)
                {
                    return ApiResponse<SchoolDto>.ErrorResult("Une école avec ce code existe déjà");
                }

                // Vérifier si l'email existe déjà
                var existingEmail = await _context.Ecoles
                    .FirstOrDefaultAsync(s => s.Email == createDto.Email);

                if (existingEmail != null)
                {
                    return ApiResponse<SchoolDto>.ErrorResult("Une école avec cet email existe déjà");
                }

                var school = _mapper.Map<Ecole>(createDto);
                _context.Ecoles.Add(school);
                await _context.SaveChangesAsync();

                var schoolDto = _mapper.Map<SchoolDto>(school);
                _logger.LogInformation("Nouvelle école créée: {SchoolCode} - {SchoolName}", school.Code, school.Nom);

                return ApiResponse<SchoolDto>.SuccessResult(schoolDto, "École créée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'école {SchoolCode}", createDto.Code);
                return ApiResponse<SchoolDto>.ErrorResult("Erreur lors de la création de l'école");
            }
        }

        public async Task<ApiResponse<SchoolDto>> UpdateSchoolAsync(int id, CreateSchoolDto updateDto)
        {
            try
            {
                var school = await _context.Ecoles.FindAsync(id);
                if (school == null || school.IsDeleted)
                {
                    return ApiResponse<SchoolDto>.ErrorResult("École non trouvée");
                }

                // Vérifier si le nouveau code existe déjà (sauf pour cette école)
                if (school.Code != updateDto.Code)
                {
                    var existingCode = await _context.Ecoles
                        .FirstOrDefaultAsync(s => s.Code == updateDto.Code && s.Id != id);

                    if (existingCode != null)
                    {
                        return ApiResponse<SchoolDto>.ErrorResult("Une école avec ce code existe déjà");
                    }
                }

                // Vérifier si le nouveau email existe déjà (sauf pour cette école)
                if (school.Email != updateDto.Email)
                {
                    var existingEmail = await _context.Ecoles
                        .FirstOrDefaultAsync(s => s.Email == updateDto.Email && s.Id != id);

                    if (existingEmail != null)
                    {
                        return ApiResponse<SchoolDto>.ErrorResult("Une école avec cet email existe déjà");
                    }
                }

                _mapper.Map(updateDto, school);
                school.UpdatedAt = DateTime.UtcNow;

                _context.Ecoles.Update(school);
                await _context.SaveChangesAsync();

                var schoolDto = _mapper.Map<SchoolDto>(school);
                _logger.LogInformation("École mise à jour: {SchoolCode} - {SchoolName}", school.Code, school.Nom);

                return ApiResponse<SchoolDto>.SuccessResult(schoolDto, "École mise à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'école {SchoolId}", id);
                return ApiResponse<SchoolDto>.ErrorResult("Erreur lors de la mise à jour de l'école");
            }
        }

        public async Task<ApiResponse> DeleteSchoolAsync(int id)
        {
            try
            {
                var school = await _context.Ecoles.FindAsync(id);
                if (school == null || school.IsDeleted)
                {
                    return ApiResponse.ErrorResult("École non trouvée");
                }

                // Soft delete
                school.IsDeleted = true;
                school.UpdatedAt = DateTime.UtcNow;

                _context.Ecoles.Update(school);
                await _context.SaveChangesAsync();

                _logger.LogInformation("École supprimée: {SchoolCode} - {SchoolName}", school.Code, school.Nom);
                return ApiResponse.SuccessResult("École supprimée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'école {SchoolId}", id);
                return ApiResponse.ErrorResult("Erreur lors de la suppression de l'école");
            }
        }

        public async Task<ApiResponse> ToggleSchoolStatusAsync(int id)
        {
            try
            {
                var school = await _context.Ecoles.FindAsync(id);
                if (school == null || school.IsDeleted)
                {
                    return ApiResponse.ErrorResult("École non trouvée");
                }

                // Ici vous pouvez ajouter une propriété IsActive à l'entité Ecole si nécessaire
                // school.IsActive = !school.IsActive;
                school.UpdatedAt = DateTime.UtcNow;

                _context.Ecoles.Update(school);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Statut de l'école modifié: {SchoolCode}", school.Code);
                return ApiResponse.SuccessResult("Statut de l'école modifié avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de statut de l'école {SchoolId}", id);
                return ApiResponse.ErrorResult("Erreur lors du changement de statut");
            }
        }

        public async Task<ApiResponse<List<SchoolDto>>> GetActiveSchoolsAsync()
        {
            try
            {
                var schools = await _context.Ecoles
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.Nom)
                    .ToListAsync();

                var schoolDtos = _mapper.Map<List<SchoolDto>>(schools);
                return ApiResponse<List<SchoolDto>>.SuccessResult(schoolDtos, "Écoles actives récupérées");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des écoles actives");
                return ApiResponse<List<SchoolDto>>.ErrorResult("Erreur lors de la récupération des écoles actives");
            }
        }
    }
}