using InstitutFroebel.Core.Entities.Base;
using InstitutFroebel.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class Ecole : BaseEntity
    {
        public string Nom { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty; // Identifiant unique (ex: FROEBEL_ABJ)
        public string Adresse { get; set; } = string.Empty;
        public string Commune { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AnneeScolaire { get; set; } = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}"; // Ex: "2024-2025"

        // Relations
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Enfant> Enfants { get; set; } = new List<Enfant>();
        public virtual ICollection<Classe> Classes { get; set; } = new List<Classe>();
        public virtual ICollection<Annonce> Annonces { get; set; } = new List<Annonce>();
        public virtual ICollection<Activite> Activites { get; set; } = new List<Activite>();
    }
}