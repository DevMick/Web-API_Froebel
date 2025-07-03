namespace InstitutFroebel.API.DTOs.Student
{
    public class EnfantDto
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string NomComplet => $"{Prenom} {Nom}";
        public DateTime DateNaissance { get; set; }
        public int Age => DateTime.Now.Year - DateNaissance.Year - (DateTime.Now.DayOfYear < DateNaissance.DayOfYear ? 1 : 0);
        public string Sexe { get; set; } = string.Empty;
        public string? Classe { get; set; }
        public string? Niveau { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string? NumeroEtudiant { get; set; } = null; // Property removed from entity
        public string ParentId { get; set; } = string.Empty; // Changed to string and uses liaison table
        public string? ParentNom { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}