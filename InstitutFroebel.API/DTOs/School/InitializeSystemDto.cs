using System.ComponentModel.DataAnnotations;

namespace InstitutFroebel.API.DTOs.School
{
    public class InitializeSystemDto
    {
        // Données de l'école (seulement les champs de la table)
        [Required(ErrorMessage = "Le nom de l'école est requis")]
        [StringLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string SchoolNom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code de l'école est requis")]
        [StringLength(50, ErrorMessage = "Le code ne peut pas dépasser 50 caractères")]
        [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "Le code ne peut contenir que des lettres majuscules, chiffres et underscores")]
        public string SchoolCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse de l'école est requise")]
        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        public string SchoolAdresse { get; set; } = string.Empty;

        [Required(ErrorMessage = "La commune de l'école est requise")]
        [StringLength(100, ErrorMessage = "La commune ne peut pas dépasser 100 caractères")]
        public string SchoolCommune { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone de l'école est requis")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string SchoolTelephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email de l'école est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        public string SchoolEmail { get; set; } = string.Empty;

        // Données du SuperAdmin (inchangé)
        [Required(ErrorMessage = "L'email du SuperAdmin est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
        public string SuperAdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe du SuperAdmin est requis")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        [DataType(DataType.Password)]
        public string SuperAdminPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [DataType(DataType.Password)]
        [Compare("SuperAdminPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string SuperAdminConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom du SuperAdmin est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string SuperAdminNom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom du SuperAdmin est requis")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string SuperAdminPrenom { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string? SuperAdminTelephone { get; set; }

        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        public string? SuperAdminAdresse { get; set; }
    }
} 