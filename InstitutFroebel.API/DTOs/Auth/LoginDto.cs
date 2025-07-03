using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InstitutFroebel.API.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "L'identifiant de l'école est requis")]
        [JsonPropertyName("ecoleId")] // ← Ajoutez cette ligne
        public int SchoolId { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}