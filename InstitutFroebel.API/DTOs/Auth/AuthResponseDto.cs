using InstitutFroebel.API.DTOs.School;
using InstitutFroebel.API.DTOs.User;

namespace InstitutFroebel.API.DTOs.Auth
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiration { get; set; }
        public UserDto? User { get; set; }
        public SchoolDto? School { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}