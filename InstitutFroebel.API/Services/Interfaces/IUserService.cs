using InstitutFroebel.API.DTOs.Common;
using InstitutFroebel.API.DTOs.User;
using InstitutFroebel.API.DTOs.Auth;

namespace InstitutFroebel.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(PagedRequest request);
        Task<ApiResponse<UserDto>> GetUserByIdAsync(string id);
        Task<ApiResponse<UserDto>> CreateUserAsync(RegisterDto registerDto, string role);
        Task<ApiResponse<UserDto>> UpdateUserAsync(string id, UpdateUserDto updateDto);
        Task<ApiResponse> DeleteUserAsync(string id);
        Task<ApiResponse> ToggleUserStatusAsync(string id);
        Task<ApiResponse<List<UserDto>>> GetUsersByRoleAsync(string role);
        Task<ApiResponse> AssignRoleAsync(string userId, string role);
        Task<ApiResponse> RemoveRoleAsync(string userId, string role);
    }
}