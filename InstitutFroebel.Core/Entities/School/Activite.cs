using InstitutFroebel.Core.Entities.Base;
using InstitutFroebel.Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstitutFroebel.Core.Entities.School
{
    public class Activite : TenantEntity
    {
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public TimeSpan? HeureDebut { get; set; }
        public TimeSpan? HeureFin { get; set; }
        public string? Lieu { get; set; }
        public string? ClasseConcernee { get; set; }

        // Relations
        public virtual ApplicationUser? CreatedBy { get; set; }
    }
}
