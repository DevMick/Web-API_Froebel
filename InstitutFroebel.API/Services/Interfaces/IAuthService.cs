using InstitutFroebel.API.DTOs.Auth;
using InstitutFroebel.API.DTOs.Common;
using InstitutFroebel.API.DTOs.User;
using InstitutFroebel.API.DTOs.School;

namespace InstitutFroebel.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, string role);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<ApiResponse> LogoutAsync(string userId);
        Task<ApiResponse<UserDto>> GetCurrentUserAsync(string userId);
        Task<ApiResponse<UserDto>> UpdateProfileAsync(string userId, UpdateUserDto updateDto);
        Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<ApiResponse> DeleteAccountAsync(string userId);
        Task<ApiResponse<List<string>>> GetUserRolesAsync(string userId);
        Task<List<SchoolDto>> GetAvailableSchoolsAsync();
    }
}