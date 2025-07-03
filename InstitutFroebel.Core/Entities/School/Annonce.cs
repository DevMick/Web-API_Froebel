using InstitutFroebel.Core.Entities.Base;
using InstitutFroebel.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class Annonce : TenantEntity
    {
        public string Titre { get; set; } = string.Empty;
        public string Contenu { get; set; } = string.Empty;
        public string Type { get; set; } = "generale"; // generale, cantine, activite, urgent, information
        public DateTime DatePublication { get; set; } = DateTime.UtcNow;
        public string? ClasseCible { get; set; } // Si annonce pour une classe spécifique
        public string? Fichiers { get; set; } // JSON des fichiers attachés
        public bool EnvoyerNotification { get; set; } = false;

        // Relations
        public virtual ApplicationUser CreatedBy { get; set; } = null!;
    }
}
