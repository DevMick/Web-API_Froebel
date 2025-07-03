using InstitutFroebel.Core.Entities.Base;
using InstitutFroebel.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class Classe : TenantEntity
    {
        public string Nom { get; set; } = string.Empty; // Ex: "CP A", "CE1 B", "Petite Section A"
        public int Effectif { get; set; } = 0;

        // Enseignant principal
        public string? EnseignantPrincipalId { get; set; }
        public virtual ApplicationUser? EnseignantPrincipal { get; set; }

        // Relations
        public virtual ICollection<Enfant> Enfants { get; set; } = new List<Enfant>();
        public virtual ICollection<Emploi> EmploisDuTemps { get; set; } = new List<Emploi>();
    }

    public class Emploi : TenantEntity
    {
        public int ClasseId { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public byte[] FichierEmploi { get; set; } = Array.Empty<byte>();
        public string AnneeScolaire { get; set; } = string.Empty;

        // Relations
        public virtual Classe Classe { get; set; } = null!;
    }
}
