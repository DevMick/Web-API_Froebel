using InstitutFroebel.Core.Entities.School;
using InstitutFroebel.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; 


namespace InstitutFroebel.Core.Entities.Identity
{
    public class ApplicationUser : IdentityUser, ITenantEntity
    {
        public int EcoleId { get; set; }
        public virtual Ecole Ecole { get; set; } = null!;

        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? Adresse { get; set; }
        public string Sexe { get; set; } = string.Empty;
        public DateTime? DateNaissance { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relations via tables de liaison - NOUVELLES
        public virtual ICollection<ParentEnfant> EnfantsAsParent { get; set; } = new List<ParentEnfant>();
        public virtual ICollection<TeacherEnfant> EnfantsAsTeacher { get; set; } = new List<TeacherEnfant>();

        // Relation directe pour les classes
        public virtual ICollection<Classe> ClassesEnseignees { get; set; } = new List<Classe>();

        public string NomComplet => $"{Prenom} {Nom}";
    }
}