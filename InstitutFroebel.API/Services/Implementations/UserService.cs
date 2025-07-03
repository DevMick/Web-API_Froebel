using InstitutFroebel.API.DTOs.Auth;
using InstitutFroebel.API.DTOs.Common;
using InstitutFroebel.API.DTOs.User;
using InstitutFroebel.API.Services.Interfaces;

namespace InstitutFroebel.API.Services.Implementations
{
    public class UserService : IUserService
    {
        public Task<ApiResponse> AssignRoleAsync(string userId, string role)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<UserDto>> CreateUserAsync(RegisterDto registerDto, string role)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse> DeleteUserAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<UserDto>> GetUserByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(PagedRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<List<UserDto>>> GetUsersByRoleAsync(string role)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse> RemoveRoleAsync(string userId, string role)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse> ToggleUserStatusAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<UserDto>> UpdateUserAsync(string id, UpdateUserDto updateDto)
        {
            throw new NotImplementedException();
        }
    }
}