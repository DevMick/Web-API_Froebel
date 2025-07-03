using InstitutFroebel.Core.Entities.Base;
using InstitutFroebel.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class Bulletin : TenantEntity
    {
        public int EnfantId { get; set; }
        public string Trimestre { get; set; } = string.Empty; // 1, 2, 3
        public string AnneeScolaire { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public byte[] FichierBulletin { get; set; } = Array.Empty<byte>();

        // Relations
        public virtual Enfant Enfant { get; set; } = null!;
    }
}
