using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace InstitutFroebel.Core.Constants
{
    public static class RoleConstants
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string Parent = "Parent";
        public const string Teacher = "Teacher";
    }
}

namespace InstitutFroebel.API.DTOs.School
{
    public class CreateSchoolDto
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code est requis")]
        [StringLength(50, ErrorMessage = "Le code ne peut pas dépasser 50 caractères")]
        [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "Le code ne peut contenir que des lettres majuscules, chiffres et underscores")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse est requise")]
        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        public string Adresse { get; set; } = string.Empty;

        [Required(ErrorMessage = "La commune est requise")]
        [StringLength(100, ErrorMessage = "La commune ne peut pas dépasser 100 caractères")]
        public string Commune { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est requis")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        public string Email { get; set; } = string.Empty;
    }
}
