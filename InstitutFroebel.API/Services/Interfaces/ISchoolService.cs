using InstitutFroebel.API.DTOs.Common;
using InstitutFroebel.API.DTOs.School;

namespace InstitutFroebel.API.Services.Interfaces
{
    public interface ISchoolService
    {
        Task<ApiResponse<PagedResult<SchoolDto>>> GetAllSchoolsAsync(PagedRequest request);
        Task<ApiResponse<SchoolDto>> GetSchoolByIdAsync(int id);
        Task<ApiResponse<SchoolDto>> GetSchoolByCodeAsync(string code);
        Task<ApiResponse<SchoolDto>> CreateSchoolAsync(CreateSchoolDto createDto);
        Task<ApiResponse<SchoolDto>> UpdateSchoolAsync(int id, CreateSchoolDto updateDto);
        Task<ApiResponse> DeleteSchoolAsync(int id);
        Task<ApiResponse> ToggleSchoolStatusAsync(int id);
        Task<ApiResponse<List<SchoolDto>>> GetActiveSchoolsAsync();
    }
}