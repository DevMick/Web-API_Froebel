using System.ComponentModel.DataAnnotations;

namespace InstitutFroebel.API.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Le token est requis")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le refresh token est requis")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}