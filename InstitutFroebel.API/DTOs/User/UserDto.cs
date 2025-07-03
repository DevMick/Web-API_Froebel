using InstitutFroebel.API.DTOs.Student;

namespace InstitutFroebel.API.DTOs.User
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public int EcoleId { get; set; }
        public string? EcoleNom { get; set; }
        public string? EcoleCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<EnfantDto> Enfants { get; set; } = new();
    }
}