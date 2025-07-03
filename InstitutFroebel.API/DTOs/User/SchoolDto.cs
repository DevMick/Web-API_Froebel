namespace InstitutFroebel.API.DTOs.School
{
    public class SchoolDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Commune { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}